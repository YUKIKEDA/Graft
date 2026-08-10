using Graft.Protocol;

namespace Graft.Core.Tests;

/// <summary>
/// Unit tests for cross-process UI session mutex (Phase 31 / X04).
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class UiSessionLockTests
{
    private static readonly TimeSpan AcquireBudget = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Second Acquire times out while another thread holds the lock.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Serialized with Sample UI tests via SampleUiCollection
    ///
    /// Steps:
    /// - Acquire lock on a background thread and keep it held
    /// - Attempt Acquire on the test thread with a short timeout
    ///
    /// Expected:
    /// - Second Acquire throws GraftException with action.timeout
    /// - Same-thread re-acquire would succeed (Windows Mutex ownership count); hence the holder thread
    /// </remarks>
    [Fact]
    public void Acquire_WhileHeld_TimesOut()
    {
        using var heldReady = new ManualResetEventSlim(false);
        using var releaseHold = new ManualResetEventSlim(false);
        Exception? holdError = null;
        var holder = new Thread(() =>
        {
            try
            {
                using var held = UiSessionLock.Acquire(AcquireBudget, CancellationToken.None);
                heldReady.Set();
                releaseHold.Wait();
            }
            catch (Exception ex)
            {
                holdError = ex;
                heldReady.Set();
            }
        })
        {
            IsBackground = true,
        };

        holder.Start();
        Assert.True(heldReady.Wait(TimeSpan.FromSeconds(30)));
        Assert.Null(holdError);

        try
        {
            var ex = Assert.Throws<GraftException>(() =>
                UiSessionLock.Acquire(TimeSpan.FromMilliseconds(300), CancellationToken.None)
            );
            Assert.Equal(GraftErrorCodes.ActionTimeout, ex.Code);
        }
        finally
        {
            releaseHold.Set();
            Assert.True(holder.Join(TimeSpan.FromSeconds(30)));
        }
    }

    /// <summary>
    /// Acquire succeeds after the previous owner disposes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Serialized with Sample UI tests via SampleUiCollection
    ///
    /// Steps:
    /// - Acquire and Dispose the first lock
    /// - Acquire again
    ///
    /// Expected:
    /// - Second Acquire returns a disposable lock without throwing
    /// </remarks>
    [Fact]
    public void Acquire_AfterRelease_Succeeds()
    {
        var first = UiSessionLock.Acquire(AcquireBudget, CancellationToken.None);
        first.Dispose();

        using var second = UiSessionLock.Acquire(AcquireBudget, CancellationToken.None);
        Assert.NotNull(second);
    }

    /// <summary>
    /// Non-positive timeout fails immediately with action.timeout.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - None
    ///
    /// Steps:
    /// - Call Acquire with TimeSpan.Zero
    ///
    /// Expected:
    /// - GraftException with action.timeout
    /// </remarks>
    [Fact]
    public void Acquire_NonPositiveTimeout_TimesOut()
    {
        var ex = Assert.Throws<GraftException>(() =>
            UiSessionLock.Acquire(TimeSpan.Zero, CancellationToken.None)
        );

        Assert.Equal(GraftErrorCodes.ActionTimeout, ex.Code);
    }
}
