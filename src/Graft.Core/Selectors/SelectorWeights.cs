namespace Graft.Core.Selectors;

/// <summary>
/// Selector scoring weights and threshold (project.md Q49; Name / ControlType raised to 60 in Phase 27 F02).
/// </summary>
public static class SelectorWeights
{
    /// <summary>
    /// Weight for an exact automation id match.
    /// </summary>
    public const int AutomationId = Graft.Protocol.SelectorWeights.AutomationId;

    /// <summary>
    /// Weight for an exact name match (alone reaches <see cref="Threshold"/>; Phase 27 F02).
    /// </summary>
    public const int Name = Graft.Protocol.SelectorWeights.Name;

    /// <summary>
    /// Weight for an exact control type match (alone reaches <see cref="Threshold"/>; Phase 27 F02).
    /// </summary>
    public const int ControlType = Graft.Protocol.SelectorWeights.ControlType;

    /// <summary>
    /// Weight for a near-path (ancestor) match.
    /// </summary>
    public const int NearPath = Graft.Protocol.SelectorWeights.NearPath;

    /// <summary>
    /// Minimum score required to accept a candidate.
    /// </summary>
    public const int Threshold = Graft.Protocol.SelectorWeights.Threshold;
}
