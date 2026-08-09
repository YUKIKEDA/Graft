using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Structured failure diagnostics (project.md Phase 2 minimum fields).
/// </summary>
/// <remarks>
/// Assembled by <c>Graft.Core</c> when Expect / Wait / actions fail.
/// The in-process agent does not attach this on every RPC response.
/// Optional attachments (operation log, tree, screenshot) are reserved for later batches.
/// </remarks>
public sealed class FailureReport
{
    /// <summary>
    /// Gets the failed step id (see <see cref="FailureSteps"/>).
    /// </summary>
    [JsonPropertyName("step")]
    public required string Step { get; init; }

    /// <summary>
    /// Gets the expected value description when applicable.
    /// </summary>
    [JsonPropertyName("expected")]
    public string? Expected { get; init; }

    /// <summary>
    /// Gets the actual value description when applicable.
    /// </summary>
    [JsonPropertyName("actual")]
    public string? Actual { get; init; }

    /// <summary>
    /// Gets a value indicating whether the failure was caused by a timeout.
    /// </summary>
    [JsonPropertyName("timedOut")]
    public bool TimedOut { get; init; }

    /// <summary>
    /// Gets the target selector snapshot for the failed step.
    /// </summary>
    [JsonPropertyName("selector")]
    public required FailureReportSelector Selector { get; init; }
}
