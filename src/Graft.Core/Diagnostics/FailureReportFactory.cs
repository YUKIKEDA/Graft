using Graft.Core.Diagnostics;
using Graft.Protocol.Messages;

namespace Graft.Core;

/// <summary>
/// Builds a <see cref="FailureReport"/> for an element query or a session operation.
/// </summary>
/// <remarks>
/// The screenshot file is <c>%TEMP%\graft-fail-&lt;guid&gt;.png</c>. Graft does not delete it.
/// That path is separate from the timeline output directory.
/// </remarks>
internal sealed class FailureReportFactory
{
    private readonly AgentConnection _connection;
    private readonly OperationLog _operationLog;
    private readonly TreeBaseline _treeBaseline;
    private readonly OperationTimeline? _timeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="FailureReportFactory"/> class.
    /// </summary>
    /// <param name="connection">Handshaken agent connection.</param>
    /// <param name="operationLog">Recent operations attached to the report.</param>
    /// <param name="treeBaseline">Last successful visual tree, used for tree diff.</param>
    /// <param name="timeline">Optional operation timeline. <see langword="null"/> when timeline capture is off.</param>
    internal FailureReportFactory(AgentConnection connection, OperationLog operationLog, TreeBaseline treeBaseline, OperationTimeline? timeline)
    {
        _connection = connection;
        _operationLog = operationLog;
        _treeBaseline = treeBaseline;
        _timeline = timeline;
    }

    /// <summary>
    /// Marks the timeline failed and attaches a best-effort report to a new <see cref="GraftException"/>.
    /// </summary>
    /// <param name="code">Stable error code.</param>
    /// <param name="message">Human-readable message.</param>
    /// <param name="step">Failed step id.</param>
    /// <param name="selector">Selector snapshot. Empty when the operation has no element selector.</param>
    /// <param name="expected">Expected value description, when applicable.</param>
    /// <param name="actual">Actual value description, when applicable.</param>
    /// <param name="timedOut">Whether the failure was a timeout.</param>
    /// <param name="treeRoot">Tree already captured for this failure. When null, getTree is tried.</param>
    /// <param name="proposeHealingCandidates">Optional candidate lookup from the captured tree.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exception to throw.</returns>
    internal async Task<GraftException> CreateAsync(
        string code,
        string message,
        string step,
        FailureReportSelector selector,
        string? expected = null,
        string? actual = null,
        bool timedOut = false,
        TreeNode? treeRoot = null,
        Func<TreeNode?, IReadOnlyList<HealingCandidate>?>? proposeHealingCandidates = null,
        Exception? innerException = null,
        CancellationToken cancellationToken = default
    )
    {
        _timeline?.MarkFailed();
        var tree = treeRoot;
        if (tree is null)
        {
            try
            {
                tree = (await _connection.GetTreeAsync(cancellationToken).ConfigureAwait(false)).Root;
            }
            catch (Exception)
            {
                // Best-effort: GraftException, OperationCanceledException, IO, etc.
                // Must not replace the original failure being reported.
            }
        }

        string? screenshotPath = null;
        try
        {
            var (_, pngBytes) = await _connection.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
            var path = Path.Combine(Path.GetTempPath(), $"graft-fail-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(path, pngBytes, cancellationToken).ConfigureAwait(false);
            screenshotPath = path;
        }
        catch (Exception)
        {
            // Best-effort attachment; keep the original failure.
        }

        var healingCandidates = proposeHealingCandidates?.Invoke(tree);
        if (healingCandidates is { Count: 0 })
        {
            healingCandidates = null;
        }

        var recent = _operationLog.Snapshot();
        return new GraftException(
            code,
            message,
            new FailureReport
            {
                Step = step,
                Expected = expected,
                Actual = actual,
                TimedOut = timedOut,
                Selector = selector,
                RecentOperations = recent.Count == 0 ? null : recent,
                Tree = tree,
                ScreenshotPath = screenshotPath,
                HealingCandidates = healingCandidates,
                TreeDiff = tree is null ? null : _treeBaseline.Diff(tree),
            },
            innerException
        );
    }
}
