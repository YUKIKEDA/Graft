using System.Windows;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Focuses a WPF element and types literal text or presses a keyboard chord via SendInput.
/// </summary>
internal sealed class WpfElementKeySender : IElementKeySender
{
    /// <inheritdoc />
    public void SendKeys(ElementSelector selector, string text)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(text);
        RunOnUiThread(() => SendKeysOnUiThread(selector, text), "sendKeys");
    }

    /// <inheritdoc />
    public void PressKeys(ElementSelector selector, string keys)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(keys);
        RunOnUiThread(() => PressKeysOnUiThread(selector, keys), "pressKeys");
    }

    private static void RunOnUiThread(Action action, string operation)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"WPF Application.Current is not available; cannot {operation}."
            );
        }

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, DispatcherPriority.Normal);
    }

    private static void SendKeysOnUiThread(ElementSelector selector, string text)
    {
        var element = ResolveActionableFrameworkElement(selector);
        WpfInputInjection.FocusAndType(element, text, clearFirst: false);
    }

    private static void PressKeysOnUiThread(ElementSelector selector, string keys)
    {
        var element = ResolveActionableFrameworkElement(selector);
        WpfInputInjection.FocusAndPress(element, keys);
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
}
