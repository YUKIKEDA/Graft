using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Invokes WPF elements on the UI dispatcher (Button / IInvokeProvider first).
/// </summary>
internal sealed class WpfElementInvoker : IElementInvoker
{
    /// <inheritdoc />
    public void Invoke(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot invoke."
            );
        }

        if (dispatcher.CheckAccess())
        {
            InvokeOnUiThread(selector);
            return;
        }

        dispatcher.Invoke(() => InvokeOnUiThread(selector), DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void BeginInvoke(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot invoke."
            );
        }

        // Do not wait: ShowDialog inside the callback would hang a sync Invoke forever.
        _ = dispatcher.BeginInvoke(() => InvokeOnUiThread(selector), DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void RightClick(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot rightClick."
            );
        }

        if (dispatcher.CheckAccess())
        {
            RightClickOnUiThread(selector);
            return;
        }

        dispatcher.Invoke(() => RightClickOnUiThread(selector), DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void DoubleClick(ElementSelector selector) =>
        RunOnUiThread(
            selector,
            static s => WpfInputInjection.DoubleClickElement(ResolveActionableFrameworkElement(s)),
            "doubleClick"
        );

    /// <inheritdoc />
    public void Hover(ElementSelector selector) =>
        RunOnUiThread(
            selector,
            static s => WpfInputInjection.HoverElement(ResolveActionableFrameworkElement(s)),
            "hover"
        );

    /// <inheritdoc />
    public void Drag(ElementSelector from, ElementSelector to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot drag."
            );
        }

        void Action()
        {
            var fromElement = ResolveActionableFrameworkElement(from);
            var toElement = ResolveActionableFrameworkElement(to);
            WpfInputInjection.DragElement(fromElement, toElement);
        }

        if (dispatcher.CheckAccess())
        {
            Action();
            return;
        }

        dispatcher.Invoke(Action, DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void ClickAt(ElementSelector selector, double offsetX, double offsetY) =>
        RunOnUiThread(
            selector,
            s =>
                WpfInputInjection.ClickAtElement(
                    ResolveActionableFrameworkElement(s),
                    offsetX,
                    offsetY
                ),
            "clickAt"
        );

    /// <inheritdoc />
    public void Wheel(ElementSelector selector, int delta) =>
        RunOnUiThread(
            selector,
            s => WpfInputInjection.WheelElement(ResolveActionableFrameworkElement(s), delta),
            "wheel"
        );

    private static void RunOnUiThread(
        ElementSelector selector,
        Action<ElementSelector> action,
        string operationName
    )
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"WPF Application.Current is not available; cannot {operationName}."
            );
        }

        if (dispatcher.CheckAccess())
        {
            action(selector);
            return;
        }

        dispatcher.Invoke(() => action(selector), DispatcherPriority.Normal);
    }

    private static void InvokeOnUiThread(ElementSelector selector)
    {
        var resolver =
            AgentServices.ElementResolver
            ?? throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "No element resolver is registered. Call WpfGraft.Use() before Agent.Start()."
            );

        var resolved = resolver.Resolve(selector);
        if (resolved.Target is Hyperlink hyperlink)
        {
            if (!hyperlink.IsEnabled)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotActionable,
                    $"Element '{resolved.AutomationId}' is not actionable (enabled=false)."
                );
            }

            hyperlink.DoClick();
            return;
        }

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

        // MenuItem: submenu headers use ExpandCollapse (not Invoke). Open via IsSubmenuOpen /
        // Click before Peer/SendInput so Menu bar File→item stays sync (Phase 20).
        if (element is MenuItem menuItem)
        {
            InvokeMenuItem(menuItem);
            return;
        }

        if (TryInvokeViaAutomationPeer(element))
        {
            return;
        }

        if (element is ButtonBase buttonBase)
        {
            buttonBase.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, buttonBase));
            return;
        }

        // Native / Peer failed — SendInput click (project.md Q40 / Q52).
        WpfInputInjection.LeftClickElement(element);
    }

    private static void InvokeMenuItem(MenuItem menuItem)
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

    private static void RightClickOnUiThread(ElementSelector selector)
    {
        var element = ResolveActionableFrameworkElement(selector);
        WpfInputInjection.RightClickElement(element);
    }

    private static FrameworkElement ResolveActionableFrameworkElement(ElementSelector selector)
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

        return element;
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

            // ButtonAutomationPeer queues OnClick via BeginInvoke; flush so Graft invoke stays sync.
            element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            return true;
        }

        return false;
    }
}
