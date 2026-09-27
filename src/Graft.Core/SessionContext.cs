using Graft.Core.Diagnostics;

namespace Graft.Core;

/// <summary>
/// Session collaborators shared by every <see cref="ElementQuery"/> created from one <see cref="GraftSession"/>.
/// </summary>
internal sealed class SessionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionContext"/> class.
    /// </summary>
    /// <param name="connection">Handshaken agent connection.</param>
    /// <param name="waitOptions">Timeouts captured when the query was created.</param>
    /// <param name="operationLog">Recent operations attached to failure reports.</param>
    /// <param name="treeBaseline">Last successful visual tree, used for tree diff.</param>
    /// <param name="timeline">Optional operation timeline. <see langword="null"/> when timeline capture is off.</param>
    /// <param name="reports">Shared failure-report factory for this session.</param>
    internal SessionContext(
        AgentConnection connection,
        WaitOptions waitOptions,
        OperationLog operationLog,
        TreeBaseline treeBaseline,
        OperationTimeline? timeline,
        FailureReportFactory reports
    )
    {
        Connection = connection;
        WaitOptions = waitOptions;
        OperationLog = operationLog;
        TreeBaseline = treeBaseline;
        Timeline = timeline;
        Reports = reports;
    }

    /// <summary>
    /// Gets the handshaken agent connection.
    /// </summary>
    internal AgentConnection Connection { get; }

    /// <summary>
    /// Gets the timeouts captured when the query was created.
    /// </summary>
    internal WaitOptions WaitOptions { get; }

    /// <summary>
    /// Gets the recent-operation log.
    /// </summary>
    internal OperationLog OperationLog { get; }

    /// <summary>
    /// Gets the tree-diff baseline.
    /// </summary>
    internal TreeBaseline TreeBaseline { get; }

    /// <summary>
    /// Gets the operation timeline, or <see langword="null"/> when timeline capture is off.
    /// </summary>
    internal OperationTimeline? Timeline { get; }

    /// <summary>
    /// Gets the shared failure-report factory.
    /// </summary>
    internal FailureReportFactory Reports { get; }
}
