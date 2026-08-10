using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Graft.Instrumentation.Screenshot;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Captures the current target WPF window as PNG on the UI dispatcher.
/// </summary>
internal sealed class WpfScreenshotProvider : IScreenshotProvider
{
    private readonly WpfWindowHost _windows;

    /// <summary>
    /// Initializes a new provider bound to <paramref name="windows"/>.
    /// </summary>
    /// <param name="windows">Window catalog / target host.</param>
    public WpfScreenshotProvider(WpfWindowHost windows)
    {
        _windows = windows;
    }

    /// <inheritdoc />
    public ScreenshotCapture Capture(ScreenshotOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new InvalidOperationException(
                "WPF Application.Current is not available; cannot capture a screenshot."
            );
        }

        if (dispatcher.CheckAccess())
        {
            return CaptureOnUiThread();
        }

        return dispatcher.Invoke(CaptureOnUiThread, DispatcherPriority.Normal);
    }

    private ScreenshotCapture CaptureOnUiThread()
    {
        Window window;
        try
        {
            window = _windows.GetTargetWindow();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }

        window.UpdateLayout();

        var dpi = VisualTreeHelper.GetDpi(window);
        var width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY));

        var bitmap = new RenderTargetBitmap(
            width,
            height,
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32
        );
        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        var pngBytes = stream.ToArray();

        return new ScreenshotCapture
        {
            Meta = new ScreenshotResult
            {
                Format = "png",
                Width = width,
                Height = height,
                ByteLength = pngBytes.Length,
            },
            PngBytes = pngBytes,
        };
    }
}
