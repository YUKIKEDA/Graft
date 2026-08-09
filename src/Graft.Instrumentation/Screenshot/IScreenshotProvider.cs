namespace Graft.Instrumentation.Screenshot;

#if GRAFT_TEST

/// <summary>
/// Framework-specific window screenshot capture used by the agent pipe server.
/// </summary>
public interface IScreenshotProvider
{
    /// <summary>
    /// Captures the target window as PNG, marshaling to the UI thread as required.
    /// </summary>
    /// <param name="options">Capture options (Phase 1: defaults only).</param>
    /// <returns>Meta and PNG bytes.</returns>
    ScreenshotCapture Capture(ScreenshotOptions options);
}

#endif
