using R3;
using SampleTodoApp.Models;

namespace SampleTodoApp.ViewModels;

public sealed class ItemDetailViewModel : IDisposable
{
    private DisposableBag _disposables;

    public ItemDetailViewModel(TodoItem item, bool isNew)
    {
        Item = item;
        IsNew = isNew;
        Title = new BindableReactiveProperty<string>(item.Title).AddTo(ref _disposables);
        Status = new BindableReactiveProperty<string>(item.Status).AddTo(ref _disposables);
        Priority = new BindableReactiveProperty<string>(item.Priority).AddTo(ref _disposables);
        DialogResult = new BindableReactiveProperty<bool?>().AddTo(ref _disposables);

        SaveCommand = Title.Select(static t => !string.IsNullOrWhiteSpace(t)).ToReactiveCommand(_ => Save()).AddTo(ref _disposables);
        CancelCommand = new ReactiveCommand(_ => DialogResult.Value = false).AddTo(ref _disposables);
    }

    public TodoItem Item { get; }

    public bool IsNew { get; }

    public string WindowTitle => IsNew ? "New Todo" : "Edit Todo";

    public BindableReactiveProperty<string> Title { get; }

    public BindableReactiveProperty<string> Status { get; }

    public BindableReactiveProperty<string> Priority { get; }

    public BindableReactiveProperty<bool?> DialogResult { get; }

    public ReactiveCommand SaveCommand { get; }

    public ReactiveCommand CancelCommand { get; }

    public IReadOnlyList<string> StatusOptions { get; } = ["未着手", "進行中", "完了"];

    public IReadOnlyList<string> PriorityOptions { get; } = ["低", "中", "高"];

    public void Dispose() => _disposables.Dispose();

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title.Value))
        {
            return;
        }

        Item.Title = Title.Value.Trim();
        Item.Status = Status.Value;
        Item.Priority = Priority.Value;
        Item.IsDone = Status.Value == "完了";
        DialogResult.Value = true;
    }
}
