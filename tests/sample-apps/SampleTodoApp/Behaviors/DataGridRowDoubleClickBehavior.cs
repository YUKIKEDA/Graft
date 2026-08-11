using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace SampleTodoApp.Behaviors;

/// <summary>
/// Invokes <see cref="Command"/> with the double-clicked row item.
/// </summary>
public sealed class DataGridRowDoubleClickBehavior : Behavior<DataGrid>
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(DataGridRowDoubleClickBehavior)
    );

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseDoubleClick += OnMouseDoubleClick;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.MouseDoubleClick -= OnMouseDoubleClick;
        base.OnDetaching();
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var command = Command;
        if (command is null)
        {
            return;
        }

        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        var item = row?.Item;
        if (item is null || !command.CanExecute(item))
        {
            return;
        }

        command.Execute(item);
    }

    private static T? FindAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
