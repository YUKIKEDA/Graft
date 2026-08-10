using Graft.Protocol;

namespace Graft.Core;

/// <summary>
/// Cross-process lock that serializes Graft UI sessions (SendInput / focus safety).
/// </summary>
/// <remarks>
/// Acquired by <see cref="Application.LaunchAsync"/> and released when the
/// <see cref="GraftSession"/> is disposed. Named <c>Local\Graft.UiSession</c>.
/// Ownership lives on a dedicated thread so <see cref="Dispose"/> is safe from
/// async continuations (Windows Mutex is thread-affine).
/// </remarks>
internal sealed class UiSessionLock : IDisposable
{
    /// <summary>
    /// Gets the Windows local mutex name used for UI session serialization.
    /// </summary>
    internal const string MutexName = @"Local\Graft.UiSession";

    /// <summary>
    /// Gets the default maximum wait when queuing for the UI session lock.
    /// </summary>
    /// <remarks>
    /// Parallel test assemblies may wait behind long SendInput sessions; this is
    /// intentionally longer than <see cref="LaunchOptions.DefaultTimeout"/> (connect/handshake).
    /// </remarks>
    internal static TimeSpan DefaultAcquireTimeout { get; } = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan PollSlice = TimeSpan.FromMilliseconds(200);

    private readonly Thread _ownerThread;
    private readonly ManualResetEventSlim _releaseGate = new(false);
    private readonly ManualResetEventSlim _releasedGate = new(false);
    private bool _disposed;

    private UiSessionLock(Thread ownerThread)
    {
        _ownerThread = ownerThread;
    }

    /// <summary>
    /// Waits up to <paramref name="timeout"/> to own the UI session mutex.
    /// </summary>
    /// <param name="timeout">Maximum wait budget for the queue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lock that must be disposed to release ownership.</returns>
    /// <exception cref="GraftException"><c>action.timeout</c> when the wait expires.</exception>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> is canceled.</exception>
    public static UiSessionLock Acquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new GraftException(
                GraftErrorCodes.ActionTimeout,
                "Timed out waiting for Graft UI session lock (timeout was non-positive)."
            );
        }

        using var acquiredGate = new ManualResetEventSlim(false);
        Exception? acquireError = null;
        UiSessionLock? sessionLock = null;

        var ownerThread = new Thread(() =>
        {
            Mutex? mutex = null;
            var ownsLock = false;
            try
            {
                mutex = new Mutex(initiallyOwned: false, MutexName);
                var deadline = DateTime.UtcNow + timeout;
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                    {
                        throw new GraftException(
                            GraftErrorCodes.ActionTimeout,
                            $"Timed out after {timeout.TotalSeconds:0.###}s waiting for Graft UI session lock ('{MutexName}')."
                        );
                    }

                    var slice = remaining < PollSlice ? remaining : PollSlice;
                    try
                    {
                        if (mutex.WaitOne(slice))
                        {
                            ownsLock = true;
                            break;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // Previous owner crashed; we now own the mutex.
                        ownsLock = true;
                        break;
                    }
                }

                sessionLock = new UiSessionLock(Thread.CurrentThread);
                acquiredGate.Set();
                sessionLock._releaseGate.Wait();
            }
            catch (Exception ex)
            {
                acquireError = ex;
                acquiredGate.Set();
            }
            finally
            {
                if (ownsLock && mutex is not null)
                {
                    try
                    {
                        mutex.ReleaseMutex();
                    }
                    catch (ApplicationException)
                    {
                        // Best-effort release if ownership was lost.
                    }
                }

                mutex?.Dispose();
                sessionLock?._releasedGate.Set();
            }
        })
        {
            IsBackground = true,
            Name = "Graft.UiSessionLock",
        };

        ownerThread.Start();

        try
        {
            while (!acquiredGate.Wait(PollSlice))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch
        {
            // Ask the owner thread to unwind if it acquired after we canceled.
            sessionLock?._releaseGate.Set();
            if (!ownerThread.Join(TimeSpan.FromSeconds(5)))
            {
                // Owner may still be inside WaitOne; it will exit when timeout elapses.
            }

            throw;
        }

        if (acquireError is not null)
        {
            ownerThread.Join(TimeSpan.FromSeconds(5));
            if (acquireError is OperationCanceledException oce)
            {
                throw oce;
            }

            if (acquireError is GraftException graftException)
            {
                throw graftException;
            }

            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "Failed to acquire Graft UI session lock.",
                acquireError
            );
        }

        return sessionLock
            ?? throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "UI session lock owner thread completed without a lock instance."
            );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _releaseGate.Set();
        if (!_releasedGate.Wait(TimeSpan.FromSeconds(30)))
        {
            // Owner thread should release promptly; avoid hanging Dispose forever.
        }

        if (!_ownerThread.Join(TimeSpan.FromSeconds(5)))
        {
            // Background owner will exit after release; do not block callers longer.
        }

        _releaseGate.Dispose();
        _releasedGate.Dispose();
    }
}
