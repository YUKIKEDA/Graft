using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Expands / collapses WPF TreeViewItem, Expander, and ComboBox drop-downs.
/// </summary>
internal sealed class WpfElementExpander : IElementExpander
{
    /// <inheritdoc />
    public void Expand(ElementSelector selector) => SetExpanded(selector, expanded: true);

    /// <inheritdoc />
    public void Collapse(ElementSelector selector) => SetExpanded(selector, expanded: false);

    private static void SetExpanded(ElementSelector selector, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot expand/collapse."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SetExpandedOnUiThread(selector, expanded);
            return;
        }

        dispatcher.Invoke(
            () => SetExpandedOnUiThread(selector, expanded),
            DispatcherPriority.Normal
        );
    }

    private static void SetExpandedOnUiThread(ElementSelector selector, bool expanded)
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

        if (TrySetViaAutomationPeer(element, expanded))
        {
            return;
        }

        switch (element)
        {
            case TreeViewItem treeItem:
                treeItem.IsExpanded = expanded;
                break;
            case Expander expander:
                expander.IsExpanded = expanded;
                break;
            case ComboBox comboBox:
                comboBox.IsDropDownOpen = expanded;
                break;
            default:
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"expand/collapse is not supported for control type '{element.GetType().Name}'."
                );
        }

        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static bool TrySetViaAutomationPeer(FrameworkElement element, bool expanded)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element);
        if (peer is null && element is UIElement uiElement)
        {
            peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        }

        if (
            peer?.GetPattern(PatternInterface.ExpandCollapse)
            is not IExpandCollapseProvider provider
        )
        {
            return false;
        }

        if (expanded)
        {
            provider.Expand();
        }
        else
        {
            provider.Collapse();
        }

        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
        return true;
    }
}
