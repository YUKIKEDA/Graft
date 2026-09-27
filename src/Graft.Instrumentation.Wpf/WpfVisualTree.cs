using System.Windows;
using System.Windows.Media;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Walks a WPF visual tree.
/// </summary>
internal static class WpfVisualTree
{
    /// <summary>
    /// Returns the first visual descendant of type <typeparamref name="T"/>, or <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">Descendant type.</typeparam>
    /// <param name="parent">Visual to search. <see langword="null"/> returns <see langword="null"/>.</param>
    /// <returns>The first match in depth-first order.</returns>
    internal static T? FindVisualChild<T>(DependencyObject? parent)
        where T : DependencyObject
    {
        if (parent is null)
        {
            return null;
        }

        foreach (var child in FindVisualChildren<T>(parent))
        {
            return child;
        }

        return null;
    }

    /// <summary>
    /// Returns every visual descendant of type <typeparamref name="T"/>, depth-first.
    /// </summary>
    /// <typeparam name="T">Descendant type.</typeparam>
    /// <param name="parent">Visual to search.</param>
    /// <returns>Matches under <paramref name="parent"/>, not including <paramref name="parent"/>.</returns>
    internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }
}
