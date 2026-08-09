namespace Graft.Instrumentation.Screenshot;

#if GRAFT_TEST

/// <summary>
/// Options for <c>screenshot</c> capture (Phase 1 defaults: main window PNG).
/// </summary>
/// <remarks>
/// JPEG / element crop parameters may be added later; unused today.
/// </remarks>
public sealed class ScreenshotOptions
{
    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static ScreenshotOptions Default { get; } = new();
}

#endif
