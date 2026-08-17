namespace Graft.Instrumentation.Screenshot;

#if GRAFT_TEST

/// <summary>
/// Framework-specific window screenshot capture used by the agent pipe server.
/// </summary>
public interface IScreenshotProvider
{
    /// <summary>
    /// Captures the target window (including open ToolTips/Popups) or an element clip as PNG, marshaling to the UI thread as required.
    /// </summary>
    /// <param name="options">Capture options (window default, or <see cref="ScreenshotOptions.Selector"/> for a clip).</param>
    /// <returns>Meta and PNG bytes.</returns>
    ScreenshotCapture Capture(ScreenshotOptions options);
}

#endif
