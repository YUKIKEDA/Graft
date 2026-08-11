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
    private bool _syncing;

    protected override void OnAttached()
    {
        base.OnAttached();

        // Mouse: take the click so three-state chrome does not fight HeaderCheckState.
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreview;

        // Graft TogglePattern changes IsChecked without raising Click.
        AssociatedObject.Checked += OnCheckStateChanged;
        AssociatedObject.Unchecked += OnCheckStateChanged;

        // Do not handle Indeterminate: partial selection updates HeaderCheckState via binding
        // and must not clear row checks.
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
        e.Handled = true;
        var vm = FindViewModel(AssociatedObject);
        if (vm is null)
        {
            return;
        }

        _syncing = true;
        try
        {
            vm.ToggleSelectAllVisible();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnCheckStateChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        var vm = FindViewModel(AssociatedObject);
        if (vm is null)
        {
            return;
        }

        _syncing = true;
        try
        {
            // true → select all; false / indeterminate → clear
            vm.SetAllVisibleChecked(AssociatedObject.IsChecked == true);
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
