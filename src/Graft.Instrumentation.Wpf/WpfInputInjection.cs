using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Interop;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Input;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// WPF helpers that map elements to screen coordinates and drive <see cref="InputInjector"/>.
/// </summary>
internal static class WpfInputInjection
{
    public static void ActivateWindow(FrameworkElement element)
    {
        var window = Window.GetWindow(element) ?? Application.Current?.MainWindow;
        if (window is null)
        {
            return;
        }

        if (!window.IsActive)
        {
            window.Activate();
        }

        var handle = new WindowInteropHelper(window).EnsureHandle();
        InputInjector.SetForegroundWindow(handle);
    }

    public static void LeftClickElement(FrameworkElement element)
    {
        ActivateWindow(element);
        var point = ResolveClickScreenPoint(element);
        InputInjector.LeftClick((int)Math.Round(point.X), (int)Math.Round(point.Y));
        element.Dispatcher.Invoke(
            static () => { },
            System.Windows.Threading.DispatcherPriority.ContextIdle
        );
    }

    public static void FocusAndType(FrameworkElement element, string text, bool clearFirst)
    {
        ActivateWindow(element);
        if (!element.Focusable)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                "Element is not focusable; cannot send keys."
            );
        }

        element.Focus();
        Keyboard.Focus(element);
        element.Dispatcher.Invoke(
            static () => { },
            System.Windows.Threading.DispatcherPriority.ContextIdle
        );

        if (clearFirst)
        {
            InputInjector.SelectAllAndDelete();
            element.Dispatcher.Invoke(
                static () => { },
                System.Windows.Threading.DispatcherPriority.ContextIdle
            );
        }

        InputInjector.TypeText(text);
        element.Dispatcher.Invoke(
            static () => { },
            System.Windows.Threading.DispatcherPriority.ContextIdle
        );
    }

    public static void FocusAndPress(FrameworkElement element, string keys)
    {
        ActivateWindow(element);
        if (!element.Focusable)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                "Element is not focusable; cannot press keys."
            );
        }

        element.Focus();
        Keyboard.Focus(element);
        element.Dispatcher.Invoke(
            static () => { },
            System.Windows.Threading.DispatcherPriority.ContextIdle
        );

        KeyChord chord;
        try
        {
            chord = KeyChordParser.Parse(keys);
        }
        catch (ArgumentException ex)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, ex.Message);
        }

        InputInjector.PressChord(chord);
        element.Dispatcher.Invoke(
            static () => { },
            System.Windows.Threading.DispatcherPriority.ContextIdle
        );
    }

    private static Point ResolveClickScreenPoint(FrameworkElement element)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element);
        if (peer is null && element is UIElement uiElement)
        {
            peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        }

        if (peer is not null)
        {
            try
            {
                var clickable = peer.GetClickablePoint();
                if (!double.IsNaN(clickable.X) && !double.IsNaN(clickable.Y))
                {
                    return clickable;
                }
            }
            catch (ElementNotAvailableException)
            {
                // Fall through to bounds center.
            }
            catch (InvalidOperationException)
            {
                // Fall through to bounds center.
            }
        }

        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "Element has empty bounds; cannot compute SendInput click point."
            );
        }

        var local = new Point(element.ActualWidth / 2, element.ActualHeight / 2);
        return element.PointToScreen(local);
    }
}
