using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Sets WPF element values on the UI dispatcher (TextBox / Slider native first).
/// </summary>
internal sealed class WpfElementValueSetter : IElementValueSetter
{
    private readonly IElementResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfElementValueSetter"/> class.
    /// </summary>
    /// <param name="resolver">Element resolver from the agent backend.</param>
    public WpfElementValueSetter(IElementResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <inheritdoc />
    public void SetValue(ElementSelector selector, string value)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(value);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot setValue.");
        }

        if (dispatcher.CheckAccess())
        {
            SetValueOnUiThread(selector, value);
            return;
        }

        dispatcher.InvokeWithTimeout(() => SetValueOnUiThread(selector, value));
    }

    private void SetValueOnUiThread(ElementSelector selector, string value)
    {
        var (element, automationId) = WpfElementResolve.ResolveActionable(_resolver, selector);

        if (WpfControlActions.TrySetValue(element, value))
        {
            return;
        }

        // Native replace first (project.md Q51 / Q114 / Q135).
        if (element is TextBox textBox)
        {
            if (textBox.IsReadOnly)
            {
                throw new ElementActionException(GraftErrorCodes.ElementNotActionable, $"Element '{automationId}' is read-only.");
            }

            textBox.Text = value;
            return;
        }

        if (element is PasswordBox passwordBox)
        {
            passwordBox.Password = value;
            return;
        }

        if (element is RichTextBox richTextBox)
        {
            if (richTextBox.IsReadOnly)
            {
                throw new ElementActionException(GraftErrorCodes.ElementNotActionable, $"Element '{automationId}' is read-only.");
            }

            SetRichTextPlain(richTextBox, value);
            return;
        }

        if (element is Slider slider)
        {
            SetSliderValue(slider, value, automationId);
            return;
        }

        if (element is DatePicker datePicker)
        {
            SetDatePickerValue(datePicker, value, automationId);
            return;
        }

        if (TrySetValueViaAutomationPeer(element, value))
        {
            return;
        }

        // Clear + SendInput type (project.md Q51).
        WpfInputInjection.FocusAndType(element, value, clearFirst: true);
    }

    private static void SetRichTextPlain(RichTextBox richTextBox, string value)
    {
        richTextBox.Document.Blocks.Clear();
        richTextBox.Document.Blocks.Add(new Paragraph(new Run(value)));
    }

    private static void SetSliderValue(Slider slider, string value, string automationId)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"setValue for Slider '{automationId}' requires an invariant-culture number (got '{value}')."
            );
        }

        if (double.IsNaN(parsed) || double.IsInfinity(parsed))
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"setValue for Slider '{automationId}' rejected non-finite value '{value}'."
            );
        }

        slider.Value = parsed;
        slider.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static void SetDatePickerValue(DatePicker datePicker, string value, string automationId)
    {
        if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"setValue for DatePicker '{automationId}' requires yyyy-MM-dd (got '{value}')."
            );
        }

        datePicker.SelectedDate = parsed.Date;
        datePicker.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
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
            throw new ElementActionException(GraftErrorCodes.ElementNotActionable, "Element ValuePattern is read-only.");
        }

        valueProvider.SetValue(value);
        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
        return true;
    }
}
