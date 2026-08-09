using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

    private static void InvokeOnUiThread(ElementSelector selector)
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

        if (TryInvokeViaAutomationPeer(element))
        {
            return;
        }

        if (element is ButtonBase buttonBase)
        {
            buttonBase.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, buttonBase));
            return;
        }

        // SendInput fallback is deferred; surface a stable failure for unsupported types.
        throw new ElementActionException(
            GraftErrorCodes.ActionFailed,
            $"Invoke is not supported for control type '{resolved.ControlType}' (SendInput fallback not implemented)."
        );
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
