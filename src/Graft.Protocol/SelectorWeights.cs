namespace Graft.Protocol;

/// <summary>
/// Selector scoring weights and threshold (Name / ControlType raised to 60 in Phase 27 F02).
/// </summary>
public static class SelectorWeights
{
    /// <summary>
    /// Weight for an exact automation id match.
    /// </summary>
    public const int AutomationId = 100;

    /// <summary>
    /// Weight for an exact name match (alone reaches <see cref="Threshold"/>).
    /// </summary>
    public const int Name = 60;

    /// <summary>
    /// Weight for an exact control type match (alone reaches <see cref="Threshold"/>).
    /// </summary>
    public const int ControlType = 60;

    /// <summary>
    /// Weight for a near-path (ancestor) match.
    /// </summary>
    public const int NearPath = 20;

    /// <summary>
    /// Minimum score required to accept a candidate.
    /// </summary>
    public const int Threshold = 60;
}
