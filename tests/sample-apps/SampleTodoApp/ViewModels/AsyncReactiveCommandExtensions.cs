using R3;

namespace SampleTodoApp.ViewModels;

internal static class AsyncReactiveCommandExtensions
{
    /// <summary>
    /// <c>ToReactiveCommand</c> ⇔ <c>ToAsyncReactiveCommand</c>.
    /// </summary>
    public static AsyncReactiveCommand ToAsyncReactiveCommand(
        this Observable<bool> canExecuteSource,
        Func<Task> execute,
        bool initialCanExecute = true,
        AwaitOperation awaitOperation = AwaitOperation.Sequential
    ) => new(canExecuteSource, execute, initialCanExecute, awaitOperation);
}
