namespace Graft.Core;

/// <summary>
/// Entry points for launching and connecting to instrumented applications.
/// </summary>
/// <remarks>
/// The documented main path is <see cref="LaunchAsync"/>.
/// <see cref="ConnectAsync"/> is a low-level API for an already-running agent
/// and is not the primary documented entry point.
/// </remarks>
public static class Application
{
    /// <summary>
    /// Starts an instrumented app with Graft environment variables, then Connect + Handshake.
    /// </summary>
    /// <remarks>
    /// Acquires the cross-process UI session lock (<c>Local\Graft.UiSession</c>) for the
    /// lifetime of the returned <see cref="GraftSession"/> so SendInput-based tests do not
    /// contend across test assemblies (Phase 31 / X04). Lock queue wait defaults to 15
    /// minutes (or <see cref="LaunchOptions.Timeout"/> if longer); connect/handshake still
    /// use <see cref="LaunchOptions.Timeout"/>.
    /// </remarks>
    /// <param name="options">Launch options (app path, optional pipe/token/timeout).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A session that owns the child process and pipe connection.</returns>
    /// <exception cref="GraftException">Launch, connection, handshake, lock wait, or timeout failed.</exception>
    public static async Task<GraftSession> LaunchAsync(
        LaunchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AppPath);

        var timeout =
            options.Timeout <= TimeSpan.Zero ? LaunchOptions.DefaultTimeout : options.Timeout;
        var pipeName = string.IsNullOrWhiteSpace(options.PipeName)
            ? "graft-" + Guid.NewGuid().ToString("N")
            : options.PipeName!;
        var token = string.IsNullOrWhiteSpace(options.Token)
            ? Guid.NewGuid().ToString("N")
            : options.Token!;
        var configuration = string.IsNullOrWhiteSpace(options.Configuration)
            ? "GraftTest"
            : options.Configuration;

        // Queue wait is separate from connect/handshake: parallel assemblies may sit behind
        // multi-minute UI sessions. Use at least DefaultAcquireTimeout.
        var lockTimeout =
            timeout > UiSessionLock.DefaultAcquireTimeout
                ? timeout
                : UiSessionLock.DefaultAcquireTimeout;
        var sessionLock = UiSessionLock.Acquire(lockTimeout, cancellationToken);
        try
        {
            var process = AppProcessLauncher.Start(options.AppPath, pipeName, token, configuration);
            try
            {
                var connection = await AgentConnection
                    .ConnectAsync(pipeName, token, timeout, cancellationToken)
                    .ConfigureAwait(false);
                return new GraftSession(process, connection, sessionLock);
            }
            catch
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        _ = process.WaitForExit(5000);
                    }
                }
                catch
                {
                    // Best-effort cleanup when Connect fails.
                }

                process.Dispose();
                throw;
            }
        }
        catch
        {
            sessionLock.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Connects to an already-running agent pipe and completes Handshake.
    /// </summary>
    /// <remarks>
    /// Low-level API. Prefer <see cref="LaunchAsync"/> for the main path.
    /// Does not acquire the UI session lock (Phase 31).
    /// </remarks>
    /// <param name="pipeName">Named pipe name (<c>GRAFT_PIPE_NAME</c>).</param>
    /// <param name="token">Connect token (<c>GRAFT_CONNECT_TOKEN</c>).</param>
    /// <param name="timeout">Overall connect + handshake budget (both phases share this).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open, handshaken <see cref="AgentConnection"/>.</returns>
    /// <exception cref="GraftException">Connection, handshake, or overall timeout failed.</exception>
    public static Task<AgentConnection> ConnectAsync(
        string pipeName,
        string token,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    ) => AgentConnection.ConnectAsync(pipeName, token, timeout, cancellationToken);
}
