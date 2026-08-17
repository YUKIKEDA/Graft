using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SampleTodoApp.Services;

public sealed class ThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    public void Apply(bool isDark)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var theme = new ResourceDictionary { Source = new Uri(isDark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative) };

        // Replace in place — Clear() briefly drops styles and causes half-light flashes.
        var merged = app.Resources.MergedDictionaries;
        if (merged.Count == 0)
        {
            merged.Add(theme);
        }
        else
        {
            merged[0] = theme;
            while (merged.Count > 1)
            {
                merged.RemoveAt(merged.Count - 1);
            }
        }

        foreach (Window window in app.Windows)
        {
            if (app.TryFindResource("AppBackgroundBrush") is Brush background)
            {
                window.Background = background;
            }

            ApplyDarkTitleBar(window, isDark);
        }
    }

    public void ApplyDarkTitleBar(Window window, bool isDark)
    {
        void Apply()
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var value = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref value, sizeof(int));
            }
        }

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
        {
            Apply();
            return;
        }

        window.SourceInitialized += (_, _) => Apply();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
