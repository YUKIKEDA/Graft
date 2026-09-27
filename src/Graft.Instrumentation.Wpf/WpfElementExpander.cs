using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
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
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot expand/collapse.");
        }

        if (dispatcher.CheckAccess())
        {
            SetExpandedOnUiThread(selector, expanded);
            return;
        }

        dispatcher.InvokeWithTimeout(() => SetExpandedOnUiThread(selector, expanded));
    }

    private static void SetExpandedOnUiThread(ElementSelector selector, bool expanded)
    {
        var (element, _) = WpfElementResolve.ResolveActionable(selector);

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
                if (TrySetIsExpandedProperty(element, expanded))
                {
                    break;
                }

                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"expand/collapse is not supported for control type '{element.GetType().Name}'."
                );
        }

        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    /// <summary>
    /// Sets a public <c>IsExpanded</c> bool property when present (e.g. WPF UI NavigationViewItem).
    /// </summary>
    private static bool TrySetIsExpandedProperty(FrameworkElement element, bool expanded)
    {
        var prop = element.GetType().GetProperty("IsExpanded");
        if (prop is null || prop.PropertyType != typeof(bool) || !prop.CanWrite)
        {
            return false;
        }

        prop.SetValue(element, expanded);
        return true;
    }

    private static bool TrySetViaAutomationPeer(FrameworkElement element, bool expanded)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element);
        if (peer is null && element is UIElement uiElement)
        {
            peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        }

        if (peer?.GetPattern(PatternInterface.ExpandCollapse) is not IExpandCollapseProvider provider)
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
