using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using SampleTodoApp.Models;
using SampleTodoApp.ViewModels;

namespace SampleTodoApp.Behaviors;

/// <summary>
/// Row checkbox toggle without affecting DataGrid row selection / focus.
/// </summary>
public sealed class TodoRowCheckBehavior : Behavior<CheckBox>
{
    private bool _syncing;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreview;

        // Graft TogglePattern changes IsChecked without raising Click.
        AssociatedObject.Checked += OnCheckStateChanged;
        AssociatedObject.Unchecked += OnCheckStateChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPreview;
        AssociatedObject.Checked -= OnCheckStateChanged;
        AssociatedObject.Unchecked -= OnCheckStateChanged;
        base.OnDetaching();
    }

    private void OnPreview(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.DataContext is not TodoItem item)
        {
            return;
        }

        e.Handled = true;
        var vm = FindViewModel(AssociatedObject);
        if (vm is null)
        {
            return;
        }

        _syncing = true;
        try
        {
            vm.ToggleItemChecked(item);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnCheckStateChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || AssociatedObject.DataContext is not TodoItem item)
        {
            return;
        }

        var vm = FindViewModel(AssociatedObject);
        if (vm is null)
        {
            return;
        }

        var want = AssociatedObject.IsChecked == true;
        if (item.IsChecked == want)
        {
            return;
        }

        _syncing = true;
        try
        {
            vm.SetItemChecked(item, want);
        }
        finally
        {
            _syncing = false;
        }
    }

    private static MainWindowViewModel? FindViewModel(DependencyObject start)
    {
        var current = start;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: MainWindowViewModel vm })
            {
                return vm;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
