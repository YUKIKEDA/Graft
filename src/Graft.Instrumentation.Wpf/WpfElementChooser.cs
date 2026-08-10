using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Selects a single WPF list/combo item by index (realizes via scroll when needed).
/// </summary>
internal sealed class WpfElementChooser : IElementChooser
{
    /// <inheritdoc />
    public void Select(ElementSelector selector, int index)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot select."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SelectOnUiThread(selector, index);
            return;
        }

        dispatcher.Invoke(() => SelectOnUiThread(selector, index), DispatcherPriority.Normal);
    }

    private static void SelectOnUiThread(ElementSelector selector, int index)
    {
        var resolver =
            AgentServices.ElementResolver
            ?? throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "No element resolver is registered. Call WpfGraft.Use() before Agent.Start()."
            );

        var resolved = resolver.Resolve(selector);
        if (resolved.Target is not FrameworkElement element)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Resolved target is not a FrameworkElement (got {resolved.Target.GetType().Name})."
            );
        }

        if (!element.IsEnabled || !element.IsVisible)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{resolved.AutomationId}' is not actionable (enabled={element.IsEnabled}, visible={element.IsVisible})."
            );
        }

        // Realize / scroll first (virtualized lists).
        _ = WpfElementScroller.ScrollListItem(element, index);

        switch (element)
        {
            case Selector sel:
                sel.SelectedIndex = index;
                break;
            default:
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"select is not supported for control type '{element.GetType().Name}'."
                );
        }

        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }
}
