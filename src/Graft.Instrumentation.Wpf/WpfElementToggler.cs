using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Toggles WPF elements on the UI dispatcher (TogglePattern / CheckBox first).
/// </summary>
internal sealed class WpfElementToggler : IElementToggler
{
    /// <inheritdoc />
    public void Toggle(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot toggle.");
        }

        if (dispatcher.CheckAccess())
        {
            ToggleOnUiThread(selector);
            return;
        }

        dispatcher.InvokeWithTimeout(() => ToggleOnUiThread(selector));
    }

    private static void ToggleOnUiThread(ElementSelector selector)
    {
        var (element, _) = WpfElementResolve.ResolveActionable(selector);

        if (WpfControlActions.TryToggle(element))
        {
            return;
        }

        // Radio: select (do not flip off). Prefer native before peer Toggle.
        if (element is RadioButton radioButton)
        {
            radioButton.IsChecked = true;
            return;
        }

        if (TryToggleViaAutomationPeer(element))
        {
            return;
        }

        if (element is ToggleButton toggleButton)
        {
            toggleButton.IsChecked = toggleButton.IsChecked != true;
            return;
        }

        WpfInputInjection.LeftClickElement(element);
    }

    private static bool TryToggleViaAutomationPeer(FrameworkElement element)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element);
        if (peer is null && element is UIElement uiElement)
        {
            peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        }

        if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
        {
            toggleProvider.Toggle();
            element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
            return true;
        }

        return false;
    }
}
