using System.Windows;
using System.Windows.Automation;
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
/// Walks Menu / ContextMenu AutomationId paths on the UI dispatcher.
/// </summary>
internal sealed class WpfMenuSelector : IMenuSelector
{
    /// <inheritdoc />
    public void SelectMenu(ElementSelector selector, string path)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var segments = SplitPath(path);
        if (segments.Length == 0)
        {
            throw new ElementResolveException(
                GraftErrorCodes.SelectorInvalid,
                "params.path must contain at least one AutomationId segment."
            );
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot selectMenu."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SelectMenuOnUiThread(selector, path, segments);
            return;
        }

        dispatcher.Invoke(
            () => SelectMenuOnUiThread(selector, path, segments),
            DispatcherPriority.Normal
        );
    }

    private static void SelectMenuOnUiThread(
        ElementSelector selector,
        string path,
        string[] segments
    )
    {
        var root = ResolveMenuRoot(selector);
        ItemsControl current = root;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var item = FindMenuItem(current, segment, path);
            if (!item.IsEnabled)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotActionable,
                    $"Menu item '{segment}' is not actionable (enabled=False) for path '{path}'."
                );
            }

            var isLast = i == segments.Length - 1;
            if (isLast)
            {
                ActivateLeaf(item);
                return;
            }

            if (!item.HasItems)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"Menu item '{segment}' has no submenu; cannot continue path '{path}'."
                );
            }

            item.IsSubmenuOpen = true;
            item.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            current = item;
        }
    }

    private static ItemsControl ResolveMenuRoot(ElementSelector selector)
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

        if (element is not Menu and not ContextMenu)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"selectMenu root must be Menu or ContextMenu (got {element.GetType().Name})."
            );
        }

        if (element is ContextMenu { IsOpen: false })
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"ContextMenu '{resolved.AutomationId}' is not open; RightClick the owner first."
            );
        }

        if (!element.IsEnabled || !element.IsVisible)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{resolved.AutomationId}' is not actionable (enabled={element.IsEnabled}, visible={element.IsVisible})."
            );
        }

        return (ItemsControl)element;
    }

    private static MenuItem FindMenuItem(ItemsControl parent, string automationId, string path)
    {
        foreach (var item in parent.Items)
        {
            var container =
                item as FrameworkElement
                ?? parent.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            if (container is not MenuItem menuItem)
            {
                continue;
            }

            var id = AutomationProperties.GetAutomationId(menuItem);
            if (string.Equals(id, automationId, StringComparison.Ordinal))
            {
                return menuItem;
            }
        }

        throw new ElementResolveException(
            GraftErrorCodes.ElementNotFound,
            $"Menu item '{automationId}' was not found under current menu for path '{path}'."
        );
    }

    private static void ActivateLeaf(MenuItem menuItem)
    {
        if (menuItem.HasItems)
        {
            menuItem.IsSubmenuOpen = true;
            menuItem.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            return;
        }

        if (TryInvokeViaAutomationPeer(menuItem))
        {
            return;
        }

        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItem));
        menuItem.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static bool TryInvokeViaAutomationPeer(FrameworkElement element)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element);
        if (peer is null && element is UIElement uiElement)
        {
            peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        }

        if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
        {
            invokeProvider.Invoke();
            return true;
        }

        return false;
    }

    private static string[] SplitPath(string path)
    {
        var parts = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return parts;
    }
}
