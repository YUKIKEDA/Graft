using System.Windows;
using R3;
using SampleTodoApp.ViewModels;

namespace SampleTodoApp.Views;

public partial class ItemDetailWindow : Window
{
    private DisposableBag _disposables;

    public ItemDetailWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) => _disposables.Dispose();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _disposables.Clear();

        if (e.NewValue is ItemDetailViewModel vm)
        {
            vm.DialogResult.Subscribe(result =>
                {
                    if (result is { } closed)
                    {
                        DialogResult = closed;
                    }
                })
                .AddTo(ref _disposables);
        }
    }
}
