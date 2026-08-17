using R3;

namespace SampleTodoApp.ViewModels;

/// <summary>
/// <see cref="ReactiveCommand"/> that runs a <see cref="Task"/> handler
/// (<c>new ReactiveCommand</c> ⇔ <c>new AsyncReactiveCommand</c>).
/// </summary>
internal sealed class AsyncReactiveCommand : ReactiveCommand
{
    public AsyncReactiveCommand(Func<Task> execute, AwaitOperation awaitOperation = AwaitOperation.Sequential)
        : base(
            async (_, _) =>
            {
                ArgumentNullException.ThrowIfNull(execute);
                await execute().ConfigureAwait(true);
            },
            awaitOperation
        ) { }

    public AsyncReactiveCommand(
        Observable<bool> canExecuteSource,
        Func<Task> execute,
        bool initialCanExecute = true,
        AwaitOperation awaitOperation = AwaitOperation.Sequential
    )
        : base(canExecuteSource ?? throw new ArgumentNullException(nameof(canExecuteSource)), initialCanExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _ = this.SubscribeAwait(async (_, _) => await execute().ConfigureAwait(true), awaitOperation);
    }
}
