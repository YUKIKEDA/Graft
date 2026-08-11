namespace Graft.Core;

/// <summary>
/// Opt-in operation timeline recording for a <see cref="GraftSession"/>.
/// </summary>
public sealed class TimelineOptions
{
    /// <summary>
    /// Gets the default per-frame dwell used by the HTML viewer (milliseconds).
    /// </summary>
    public const int DefaultFrameDelayMilliseconds = 800;

    /// <summary>
    /// Gets the directory that receives <c>frames/</c>, <c>timeline.json</c>, and <c>index.html</c>.
    /// </summary>
    /// <remarks>Required when timeline recording is enabled.</remarks>
    public required string OutputDirectory { get; init; }

    /// <summary>
    /// Gets whether artifacts are always kept or only after a Graft failure.
    /// </summary>
    public TimelineRetention Retention { get; init; } = TimelineRetention.Always;

    /// <summary>
    /// Gets the HTML viewer default frame delay in milliseconds.
    /// </summary>
    public int FrameDelayMilliseconds { get; init; } = DefaultFrameDelayMilliseconds;
}
