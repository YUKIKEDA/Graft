using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using SampleTodoApp.ViewModels;

namespace SampleTodoApp.Behaviors;

/// <summary>
/// Header checkbox: toggle check-all for visible rows (independent of DataGrid selection).
/// </summary>
public sealed class TodoSelectAllBehavior : Behavior<CheckBox>
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
        e.Handled = true;
        FindViewModel(AssociatedObject)?.ToggleSelectAllVisible();
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
