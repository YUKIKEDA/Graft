using System.Windows;
using System.Windows.Threading;
using Graft.Instrumentation.Elements;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Resolves live WPF elements on the UI dispatcher via <see cref="WpfVisualTreeWalker"/>.
/// </summary>
internal sealed class WpfElementResolver : IElementResolver
{
    private readonly WpfWindowHost _windows;

    /// <summary>
    /// Initializes a new resolver bound to <paramref name="windows"/>.
    /// </summary>
    /// <param name="windows">Window catalog / target host.</param>
    public WpfElementResolver(WpfWindowHost windows)
    {
        _windows = windows;
    }

    /// <inheritdoc />
    public ResolvedElement Resolve(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementResolveException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot resolve elements.");
        }

        if (dispatcher.CheckAccess())
        {
            return ResolveOnUiThread(selector);
        }

        return dispatcher.Invoke(() => ResolveOnUiThread(selector), DispatcherPriority.Normal);
    }

    private ResolvedElement ResolveOnUiThread(ElementSelector selector)
    {
        Window window;
        try
        {
            window = _windows.GetTargetWindow();
        }
        catch (InvalidOperationException ex)
        {
            throw new ElementResolveException(GraftErrorCodes.WindowNotFound, ex.Message);
        }

        window.UpdateLayout();
        return WpfVisualTreeWalker.Resolve(window, selector);
    }
}
