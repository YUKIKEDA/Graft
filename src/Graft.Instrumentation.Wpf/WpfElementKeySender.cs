using System.Windows;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Focuses a WPF element and types literal text via SendInput.
/// </summary>
internal sealed class WpfElementKeySender : IElementKeySender
{
    /// <inheritdoc />
    public void SendKeys(ElementSelector selector, string text)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(text);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot sendKeys."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SendKeysOnUiThread(selector, text);
            return;
        }

        dispatcher.Invoke(() => SendKeysOnUiThread(selector, text), DispatcherPriority.Normal);
    }

    private static void SendKeysOnUiThread(ElementSelector selector, string text)
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

        WpfInputInjection.FocusAndType(element, text, clearFirst: false);
    }
}
