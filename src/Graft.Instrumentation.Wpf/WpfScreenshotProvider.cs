using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Captures the current target WPF window (including open overlays) or an element clip as PNG on the UI dispatcher.
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
            return CaptureOnUiThread(options);
        }

        return dispatcher.Invoke(() => CaptureOnUiThread(options), DispatcherPriority.Normal);
    }

    private ScreenshotCapture CaptureOnUiThread(ScreenshotOptions options)
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

        if (!options.HasElementSelector)
        {
            return CaptureWindowWithOverlays(window);
        }

        var resolved = WpfVisualTreeWalker.ResolveForScreenshot(window, options.Selector!);
        if (resolved.Target is not FrameworkElement element)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{Describe(resolved)}' is not a visual; cannot screenshot."
            );
        }

        var related = OrderForComposite(CollectRelatedVisuals(element));
        var layers = new List<(FrameworkElement Element, BitmapSource Bitmap)>(related.Count);
        foreach (var visual in related)
        {
            visual.UpdateLayout();
            var captureRoot = FindCaptureRoot(visual, window);
            if (captureRoot is UIElement rootElement)
            {
                rootElement.UpdateLayout();
            }

            var source = RenderVisual(captureRoot);
            var cropped = CropToBitmap(source, captureRoot, visual, Describe(resolved));
            layers.Add((visual, cropped));
        }

        return layers.Count == 1 ? Encode(layers[0].Bitmap) : Composite(layers);
    }

    private static ScreenshotCapture CaptureWindowWithOverlays(Window window)
    {
        var overlays = new List<FrameworkElement>();
        CollectOpenOverlays(window, [], overlays);
        var windowBitmap = RenderVisual(window);
        if (overlays.Count == 0)
        {
            return Encode(windowBitmap);
        }

        var layers = new List<(FrameworkElement Element, BitmapSource Bitmap)>
        {
            (window, windowBitmap),
        };
        foreach (var overlay in overlays)
        {
            if (TryCaptureVisual(window, overlay, "overlay", out var bitmap))
            {
                layers.Add((overlay, bitmap));
            }
        }

        return layers.Count == 1 ? Encode(windowBitmap) : Composite(layers);
    }

    private static bool TryCaptureVisual(
        Window window,
        FrameworkElement visual,
        string describe,
        out BitmapSource bitmap
    )
    {
        try
        {
            visual.UpdateLayout();
            var captureRoot = FindCaptureRoot(visual, window);
            if (captureRoot is UIElement rootElement)
            {
                rootElement.UpdateLayout();
            }

            var source = RenderVisual(captureRoot);
            bitmap = CropToBitmap(source, captureRoot, visual, describe);
            return true;
        }
        catch (ElementActionException)
        {
            bitmap = null!;
            return false;
        }
        catch (InvalidOperationException)
        {
            bitmap = null!;
            return false;
        }
    }

    private static void CollectOpenOverlays(
        DependencyObject current,
        HashSet<DependencyObject> visited,
        List<FrameworkElement> overlays
    )
    {
        if (!visited.Add(current))
        {
            return;
        }

        if (current is FrameworkElement element)
        {
            if (element.ToolTip is ToolTip { IsOpen: true } ownedTip)
            {
                AddUnique(overlays, ownedTip);
            }

            if (element.ContextMenu is { IsOpen: true } ownedMenu)
            {
                AddUnique(overlays, ownedMenu);
            }

            if (current is Popup { IsOpen: true, Child: FrameworkElement popupChild })
            {
                AddUnique(overlays, popupChild);
            }

            if (current is ContextMenu { IsOpen: true } contextMenu)
            {
                AddUnique(overlays, contextMenu);
            }
        }

        if (current is Visual)
        {
            var visualCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < visualCount; i++)
            {
                CollectOpenOverlays(VisualTreeHelper.GetChild(current, i), visited, overlays);
            }
        }

        foreach (var child in LogicalTreeHelper.GetChildren(current))
        {
            if (child is DependencyObject dependency)
            {
                CollectOpenOverlays(dependency, visited, overlays);
            }
        }
    }

    private static List<FrameworkElement> CollectRelatedVisuals(FrameworkElement element)
    {
        var related = new List<FrameworkElement>();
        AddUnique(related, element);
        CollectOpenOverlays(element, [], related);

        if (element is ToolTip { PlacementTarget: FrameworkElement tipHost })
        {
            AddUnique(related, tipHost);
        }

        var popup = FindHostingPopup(element);
        if (popup is { IsOpen: true, PlacementTarget: FrameworkElement popupHost })
        {
            AddUnique(related, popupHost);
        }

        return related;
    }

    private static List<FrameworkElement> OrderForComposite(List<FrameworkElement> related)
    {
        var bases = new List<FrameworkElement>();
        var overlays = new List<FrameworkElement>();
        foreach (var visual in related)
        {
            if (visual is ToolTip or ContextMenu || FindHostingPopup(visual) is not null)
            {
                overlays.Add(visual);
            }
            else
            {
                bases.Add(visual);
            }
        }

        if (bases.Count == 0)
        {
            return related;
        }

        var ordered = new List<FrameworkElement>(related.Count);
        ordered.AddRange(bases);
        ordered.AddRange(overlays);
        return ordered;
    }

    private static void AddUnique(List<FrameworkElement> list, FrameworkElement element)
    {
        if (element.ActualWidth <= 0 && element.ActualHeight <= 0)
        {
            return;
        }

        foreach (var existing in list)
        {
            if (ReferenceEquals(existing, element))
            {
                return;
            }
        }

        list.Add(element);
    }

    private static Popup? FindHostingPopup(DependencyObject element)
    {
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is Popup popup)
            {
                return popup;
            }

            current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static Visual FindCaptureRoot(Visual element, Window window)
    {
        DependencyObject? current = element;
        Visual lastVisual = element;
        while (current is not null)
        {
            if (ReferenceEquals(current, window))
            {
                return window;
            }

            if (current is Visual visual)
            {
                lastVisual = visual;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return lastVisual;
    }

    private static RenderTargetBitmap RenderVisual(Visual visual)
    {
        var dpi = VisualTreeHelper.GetDpi(visual);
        var dipWidth = visual is FrameworkElement framework
            ? framework.ActualWidth
            : (visual as UIElement)?.RenderSize.Width ?? 0;
        var dipHeight = visual is FrameworkElement frameworkHeight
            ? frameworkHeight.ActualHeight
            : (visual as UIElement)?.RenderSize.Height ?? 0;

        var width = Math.Max(1, (int)Math.Ceiling(Math.Max(dipWidth, 0) * dpi.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(Math.Max(dipHeight, 0) * dpi.DpiScaleY));

        var bitmap = new RenderTargetBitmap(
            width,
            height,
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32
        );
        bitmap.Render(visual);
        return bitmap;
    }

    private static BitmapSource CropToBitmap(
        RenderTargetBitmap source,
        Visual captureRoot,
        FrameworkElement element,
        string describe
    )
    {
        if (element.ActualWidth <= 0 && element.ActualHeight <= 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{describe}' has empty bounds; cannot screenshot."
            );
        }

        Rect dipBounds;
        try
        {
            var transform = element.TransformToVisual(captureRoot);
            dipBounds = transform.TransformBounds(
                new Rect(0, 0, element.ActualWidth, element.ActualHeight)
            );
        }
        catch (InvalidOperationException)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{describe}' is not in the capture visual; cannot screenshot."
            );
        }

        var dpi = VisualTreeHelper.GetDpi(captureRoot);
        var pixelBounds = new Rect(
            dipBounds.X * dpi.DpiScaleX,
            dipBounds.Y * dpi.DpiScaleY,
            dipBounds.Width * dpi.DpiScaleX,
            dipBounds.Height * dpi.DpiScaleY
        );
        var bitmapBounds = new Rect(0, 0, source.PixelWidth, source.PixelHeight);
        var intersect = Rect.Intersect(pixelBounds, bitmapBounds);
        if (intersect.IsEmpty || intersect.Width < 1 || intersect.Height < 1)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{describe}' does not intersect the capture bitmap; cannot screenshot."
            );
        }

        var x = (int)Math.Floor(intersect.X);
        var y = (int)Math.Floor(intersect.Y);
        var width = (int)Math.Ceiling(intersect.Width);
        var height = (int)Math.Ceiling(intersect.Height);
        if (x < 0)
        {
            width += x;
            x = 0;
        }

        if (y < 0)
        {
            height += y;
            y = 0;
        }

        if (x + width > source.PixelWidth)
        {
            width = source.PixelWidth - x;
        }

        if (y + height > source.PixelHeight)
        {
            height = source.PixelHeight - y;
        }

        if (width < 1 || height < 1)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{describe}' does not intersect the capture bitmap; cannot screenshot."
            );
        }

        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, width, height));
        if (cropped.CanFreeze)
        {
            cropped.Freeze();
        }

        return cropped;
    }

    private static ScreenshotCapture Composite(
        IReadOnlyList<(FrameworkElement Element, BitmapSource Bitmap)> layers
    )
    {
        var union = Rect.Empty;
        var placements = new List<(BitmapSource Bitmap, Point Screen)>(layers.Count);
        foreach (var (element, bitmap) in layers)
        {
            Point topLeft;
            try
            {
                topLeft = element.PointToScreen(new Point(0, 0));
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var screen = new Rect(topLeft.X, topLeft.Y, bitmap.PixelWidth, bitmap.PixelHeight);
            union = union.IsEmpty ? screen : Rect.Union(union, screen);
            placements.Add((bitmap, topLeft));
        }

        if (placements.Count == 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                "Element overlay screenshot has no screen-visible layers."
            );
        }

        if (placements.Count == 1)
        {
            return Encode(placements[0].Bitmap);
        }

        var destW = Math.Max(1, (int)Math.Ceiling(union.Width));
        var destH = Math.Max(1, (int)Math.Ceiling(union.Height));
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, destW, destH));
            foreach (var (bitmap, screen) in placements)
            {
                var x = screen.X - union.X;
                var y = screen.Y - union.Y;
                context.DrawImage(bitmap, new Rect(x, y, bitmap.PixelWidth, bitmap.PixelHeight));
            }
        }

        var dest = new RenderTargetBitmap(destW, destH, 96, 96, PixelFormats.Pbgra32);
        dest.Render(visual);
        return Encode(dest, destW, destH);
    }

    private static ScreenshotCapture Encode(
        BitmapSource bitmap,
        int? width = null,
        int? height = null
    )
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        var pngBytes = stream.ToArray();
        var pixelWidth = width ?? bitmap.PixelWidth;
        var pixelHeight = height ?? bitmap.PixelHeight;

        return new ScreenshotCapture
        {
            Meta = new ScreenshotResult
            {
                Format = "png",
                Width = pixelWidth,
                Height = pixelHeight,
                ByteLength = pngBytes.Length,
            },
            PngBytes = pngBytes,
        };
    }

    private static string Describe(ResolvedElement resolved) =>
        string.IsNullOrWhiteSpace(resolved.AutomationId)
            ? $"runtimeId={resolved.RuntimeId}"
            : resolved.AutomationId;
}
