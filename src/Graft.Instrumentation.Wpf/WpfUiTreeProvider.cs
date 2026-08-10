using System.Windows;
using System.Windows.Threading;
using Graft.Instrumentation.Tree;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Captures the WPF visual tree on the UI dispatcher.
/// </summary>
internal sealed class WpfUiTreeProvider : IUiTreeProvider
{
    private readonly WpfWindowHost _windows;

    /// <summary>
    /// Initializes a new provider bound to <paramref name="windows"/>.
    /// </summary>
    /// <param name="windows">Window catalog / target host.</param>
    public WpfUiTreeProvider(WpfWindowHost windows)
    {
        _windows = windows;
    }

    /// <inheritdoc />
    public GetTreeResult GetTree(GetTreeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new InvalidOperationException(
                "WPF Application.Current is not available; cannot capture the UI tree."
            );
        }

        if (dispatcher.CheckAccess())
        {
            return CaptureOnUiThread(options);
        }

        return dispatcher.Invoke(() => CaptureOnUiThread(options), DispatcherPriority.Normal);
    }

    private GetTreeResult CaptureOnUiThread(GetTreeOptions options)
    {
        var window = _windows.GetTargetWindow();
        window.UpdateLayout();
        return WpfVisualTreeWalker.Capture(window, options);
    }
}
