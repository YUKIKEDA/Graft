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
/// Sets WPF element values on the UI dispatcher (TextBox native replace first).
/// </summary>
internal sealed class WpfElementValueSetter : IElementValueSetter
{
    /// <inheritdoc />
    public void SetValue(ElementSelector selector, string value)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(value);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot setValue."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SetValueOnUiThread(selector, value);
            return;
        }

        dispatcher.Invoke(() => SetValueOnUiThread(selector, value), DispatcherPriority.Normal);
    }

    private static void SetValueOnUiThread(ElementSelector selector, string value)
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

        // Native replace first (project.md Q51).
        if (element is TextBox textBox)
        {
            if (textBox.IsReadOnly)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotActionable,
                    $"Element '{resolved.AutomationId}' is read-only."
                );
            }

            textBox.Text = value;
            return;
        }

        if (TrySetValueViaAutomationPeer(element, value))
        {
            return;
        }

        // Clear + SendInput fallback is deferred.
        throw new ElementActionException(
            GraftErrorCodes.ActionFailed,
            $"setValue is not supported for control type '{resolved.ControlType}' (SendInput fallback not implemented)."
        );
    }

    private static bool TrySetValueViaAutomationPeer(FrameworkElement element, string value)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element);
        if (peer is null && element is UIElement uiElement)
        {
            peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        }

        if (peer?.GetPattern(PatternInterface.Value) is not IValueProvider valueProvider)
        {
            return false;
        }

        if (valueProvider.IsReadOnly)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                "Element ValuePattern is read-only."
            );
        }

        valueProvider.SetValue(value);
        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
        return true;
    }
}
