namespace Graft.Core;

/// <summary>
/// Entry points for launching and connecting to instrumented applications.
/// </summary>
/// <remarks>
/// The documented main path will be <c>LaunchAsync</c> (M2 Batch 2).
/// <see cref="ConnectAsync"/> is a low-level API for an already-running agent.
/// </remarks>
public static class Application
{
    /// <summary>
    /// Connects to an already-running agent pipe and completes Handshake.
    /// </summary>
    /// <param name="pipeName">Named pipe name (<c>GRAFT_PIPE_NAME</c>).</param>
    /// <param name="token">Connect token (<c>GRAFT_CONNECT_TOKEN</c>).</param>
    /// <param name="timeout">Overall connect + handshake budget.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open, handshaken <see cref="AgentConnection"/>.</returns>
    /// <exception cref="GraftException">Connection or handshake failed.</exception>
    public static Task<AgentConnection> ConnectAsync(
        string pipeName,
        string token,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    ) => AgentConnection.ConnectAsync(pipeName, token, timeout, cancellationToken);
}
