using System.Text.Json.Serialization;
using Graft.Protocol.Messages;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Structured failure diagnostics (project.md Phase 2).
/// </summary>
/// <remarks>
/// Assembled by <c>Graft.Core</c> when Expect / Wait / actions fail.
/// The in-process agent does not attach this on every RPC response.
/// Optional attachments (operation log, tree, screenshot path) are best-effort on failure.
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

    /// <summary>
    /// Gets recent controller operations leading up to the failure (oldest first).
    /// </summary>
    [JsonPropertyName("recentOperations")]
    public IReadOnlyList<OperationLogEntry>? RecentOperations { get; init; }

    /// <summary>
    /// Gets the UI tree root captured around the failure, when available.
    /// </summary>
    [JsonPropertyName("tree")]
    public TreeNode? Tree { get; init; }

    /// <summary>
    /// Gets a temp-file path to a PNG screenshot captured on failure, when available.
    /// </summary>
    [JsonPropertyName("screenshotPath")]
    public string? ScreenshotPath { get; init; }

    /// <summary>
    /// Gets ranked alternate selectors suggested when the intended selector failed (Phase 4).
    /// </summary>
    [JsonPropertyName("healingCandidates")]
    public IReadOnlyList<HealingCandidate>? HealingCandidates { get; init; }
}
