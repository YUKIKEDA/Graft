using System.Text.Json.Serialization;
using Graft.Protocol.Messages;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Structured failure diagnostics (project.md Phase 2).
/// </summary>
/// <remarks>
/// Assembled by <c>Graft.Core</c> when an element query or a session operation fails.
/// The in-process agent does not attach this on every RPC response.
/// Optional attachments (operation log, tree, screenshot path) are best-effort on failure.
/// A session operation that has no element selector stores an empty selector.
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
    /// Gets the path of a PNG captured on failure, when available.
    /// </summary>
    /// <remarks>
    /// The file is <c>%TEMP%\graft-fail-&lt;guid&gt;.png</c>. Graft does not delete it.
    /// This path is separate from the timeline output directory.
    /// </remarks>
    [JsonPropertyName("screenshotPath")]
    public string? ScreenshotPath { get; init; }

    /// <summary>
    /// Gets ranked alternate selectors suggested when the intended selector failed (Phase 4).
    /// </summary>
    [JsonPropertyName("healingCandidates")]
    public IReadOnlyList<HealingCandidate>? HealingCandidates { get; init; }

    /// <summary>
    /// Gets the diff against the last successful tree, when a baseline exists.
    /// </summary>
    [JsonPropertyName("treeDiff")]
    public TreeDiff? TreeDiff { get; init; }

    /// <summary>
    /// Gets the individual reports collected by a soft-assert scope.
    /// </summary>
    /// <remarks>
    /// Set only on the aggregate report thrown from <c>SoftAssertScope</c>.
    /// Each entry is the report from one failed check. Absent on a normal single failure.
    /// </remarks>
    [JsonPropertyName("failures")]
    public IReadOnlyList<FailureReport>? Failures { get; init; }
}
