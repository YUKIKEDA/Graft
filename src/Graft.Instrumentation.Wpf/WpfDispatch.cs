using System.Windows;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Marshals agent work from the pipe I/O thread to the WPF UI thread with a bounded wait.
/// </summary>
/// <remarks>
/// A plain <see cref="Dispatcher.Invoke(Action, DispatcherPriority)"/> blocks forever when the UI
/// thread is stuck (an unpatched modal dialog, an app deadlock), which also pins the pipe thread so
/// the agent can never recover. These helpers wait at most <see cref="Timeout"/> and then fail with
/// <c>action.timeout</c>, freeing the pipe thread to report the hang.
/// </remarks>
internal static class WpfDispatch
{
    /// <summary>
    /// Maximum time the pipe thread waits for the UI thread to run one operation.
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread and waits at most <see cref="Timeout"/>.
    /// </summary>
    /// <param name="dispatcher">UI dispatcher.</param>
    /// <param name="action">Work to run.</param>
    /// <exception cref="ElementActionException">The UI thread did not complete the work in time (<c>action.timeout</c>).</exception>
    public static void InvokeWithTimeout(this Dispatcher dispatcher, Action action) =>
        _ = dispatcher.InvokeWithTimeout(() =>
        {
            action();
            return true;
        });

    /// <summary>
    /// Runs <paramref name="func"/> on the UI thread, waits at most <see cref="Timeout"/>, and returns its result.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="dispatcher">UI dispatcher.</param>
    /// <param name="func">Work to run.</param>
    /// <returns>The value returned by <paramref name="func"/>.</returns>
    /// <exception cref="ElementActionException">The UI thread did not complete the work in time (<c>action.timeout</c>).</exception>
    public static T InvokeWithTimeout<T>(this Dispatcher dispatcher, Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(func);

        if (dispatcher.CheckAccess())
        {
            return func();
        }

        var operation = dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
        if (operation.Wait(Timeout) != DispatcherOperationStatus.Completed)
        {
            // Abort only succeeds while still queued; a running callback keeps going, but the pipe thread is released.
            _ = operation.Abort();
            throw new ElementActionException(
                GraftErrorCodes.ActionTimeout,
                $"The WPF UI thread did not respond within {Timeout.TotalSeconds:0}s (blocked by a modal dialog or a deadlock?)."
            );
        }

        // Rethrows the callback's exception unwrapped, like a synchronous Dispatcher.Invoke.
        return operation.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Pumps the dispatcher to <see cref="DispatcherPriority.ContextIdle"/> so a layout pass can finish.
    /// </summary>
    /// <param name="element">Element whose dispatcher is pumped.</param>
    internal static void Idle(DispatcherObject element) => element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);

    /// <summary>
    /// Runs <paramref name="action"/> on the WPF application dispatcher.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="action">Work to run.</param>
    /// <param name="unavailableMessage">Message when <c>Application.Current</c> has no dispatcher.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    /// <exception cref="ElementActionException">No dispatcher is available, or the UI thread did not finish in time.</exception>
    internal static T InvokeOnUi<T>(Func<T> action, string unavailableMessage)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, unavailableMessage);
        }

        if (dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.InvokeWithTimeout(action);
    }
}
