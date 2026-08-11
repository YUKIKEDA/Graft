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
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreview;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPreview;
        base.OnDetaching();
    }

    private void OnPreview(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.DataContext is not TodoItem item)
        {
            return;
        }

        e.Handled = true;
        FindViewModel(AssociatedObject)?.ToggleItemChecked(item);
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
