using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// A suggested alternate selector produced when the intended selector fails to resolve.
/// </summary>
public sealed class HealingCandidate
{
    /// <summary>
    /// Gets the score that the candidate selector achieved against the tree.
    /// </summary>
    [JsonPropertyName("score")]
    public required int Score { get; init; }

    /// <summary>
    /// Gets the alternate selector criteria.
    /// </summary>
    [JsonPropertyName("selector")]
    public required FailureReportSelector Selector { get; init; }

    /// <summary>
    /// Gets a short reason (e.g. <c>relaxed</c>, <c>stableIdentity</c>).
    /// </summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}
