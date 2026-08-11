using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace SampleTodoApp.Behaviors;

/// <summary>
/// Sets <see cref="Window.DialogResult"/> from a bound nullable bool (view-model close signal).
/// </summary>
public sealed class DialogResultBehavior : Behavior<Window>
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(bool?),
        typeof(DialogResultBehavior),
        new PropertyMetadata(null, OnValueChanged)
    );

    public bool? Value
    {
        get => (bool?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DialogResultBehavior { AssociatedObject: { } window })
        {
            return;
        }

        if (e.NewValue is not bool result || !window.IsLoaded || !window.IsVisible)
        {
            return;
        }

        window.DialogResult = result;
    }
}
