using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Screenshot;

#if GRAFT_TEST

/// <summary>
/// In-process screenshot: JSON meta plus PNG bytes for the follow-up wire frame.
/// </summary>
public sealed class ScreenshotCapture
{
    /// <summary>
    /// Gets the wire JSON meta (<c>format</c>, size, <c>byteLength</c>).
    /// </summary>
    public required ScreenshotResult Meta { get; init; }

    /// <summary>
    /// Gets the PNG payload written as the raw binary frame after the JSON response.
    /// </summary>
    public required byte[] PngBytes { get; init; }
}

#endif
