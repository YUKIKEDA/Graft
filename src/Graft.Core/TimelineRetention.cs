namespace Graft.Core;

/// <summary>
/// Whether an operation timeline is kept after the session ends.
/// </summary>
public enum TimelineRetention
{
    /// <summary>
    /// Always write PNG frames and the HTML viewer (default).
    /// </summary>
    Always = 0,

    /// <summary>
    /// Keep artifacts only when a Graft failure was recorded; discard on clean dispose.
    /// </summary>
    OnFailure = 1,
}
