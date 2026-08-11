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
    /// <param name="options">Launch options (app path, optional pipe/token/timeout).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A session that owns the child process and pipe connection.</returns>
    /// <exception cref="GraftException">Launch, connection, handshake, or timeout failed.</exception>
    public static async Task<GraftSession> LaunchAsync(
        LaunchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AppPath);
        if (options.Timeline is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Timeline.OutputDirectory);
        }

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

        var process = AppProcessLauncher.Start(options.AppPath, pipeName, token, configuration);
        try
        {
            var connection = await AgentConnection
                .ConnectAsync(pipeName, token, timeout, cancellationToken)
                .ConfigureAwait(false);
            return new GraftSession(process, connection, options.Timeline);
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

    /// <summary>
    /// Connects to an already-running agent pipe and completes Handshake.
    /// </summary>
    /// <remarks>
    /// Low-level API. Prefer <see cref="LaunchAsync"/> for the main path.
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
