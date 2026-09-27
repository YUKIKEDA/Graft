using Graft.Protocol;

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
    public static async Task<GraftSession> LaunchAsync(LaunchOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AppPath);
        if (options.Timeline is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Timeline.OutputDirectory);
        }

        var timeout = options.Timeout <= TimeSpan.Zero ? LaunchOptions.DefaultTimeout : options.Timeout;
        var pipeName = string.IsNullOrWhiteSpace(options.PipeName) ? "graft-" + Guid.NewGuid().ToString("N") : options.PipeName!;
        // The connect token is the handshake secret: use a CSPRNG (128 bits, hex) rather than Guid.NewGuid().
        var token = string.IsNullOrWhiteSpace(options.Token)
            ? System.Security.Cryptography.RandomNumberGenerator.GetHexString(32, lowercase: true)
            : options.Token!;
        var configuration = string.IsNullOrWhiteSpace(options.Configuration) ? "GraftTest" : options.Configuration;

        var process = AppProcessLauncher.Start(options.AppPath, pipeName, token, configuration, options.Environment, out var outputTail);
        try
        {
            var connection = await ConnectWhileAliveAsync(process, outputTail, options.AppPath, pipeName, token, timeout, cancellationToken)
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
    /// Connect + Handshake, failing fast with diagnostics when the launched process exits first.
    /// </summary>
    private static async Task<AgentConnection> ConnectWhileAliveAsync(
        System.Diagnostics.Process process,
        AppProcessLauncher.OutputTail outputTail,
        string appPath,
        string pipeName,
        string token,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connectTask = AgentConnection.ConnectAsync(pipeName, token, timeout, raceCts.Token);
        var exitTask = process.WaitForExitAsync(raceCts.Token);

        var first = await Task.WhenAny(connectTask, exitTask).ConfigureAwait(false);
        if (first == connectTask)
        {
            raceCts.Cancel();
            return await connectTask.ConfigureAwait(false);
        }

        if (!exitTask.IsCompletedSuccessfully)
        {
            // Caller cancelled; let the connect task surface the cancellation.
            return await connectTask.ConfigureAwait(false);
        }

        raceCts.Cancel();
        try
        {
            var late = await connectTask.ConfigureAwait(false);
            await late.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is GraftException or OperationCanceledException)
        {
            // Expected: the process is gone, so the connect attempt is abandoned.
        }

        throw new GraftException(GraftErrorCodes.ActionFailed, BuildEarlyExitMessage(process, outputTail, appPath));
    }

    private static string BuildEarlyExitMessage(System.Diagnostics.Process process, AppProcessLauncher.OutputTail outputTail, string appPath)
    {
        string exitCode;
        try
        {
            exitCode = process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (InvalidOperationException)
        {
            exitCode = "unknown";
        }

        var message = new System.Text.StringBuilder()
            .Append("The application process exited (exit code ")
            .Append(exitCode)
            .Append(") before the Graft agent accepted a connection: ")
            .AppendLine(appPath)
            .AppendLine("Common causes:")
            .AppendLine("- The app was not built with the GraftTest configuration, so Agent.Start() is compiled out.")
            .AppendLine("- Agent.Start() is not called at startup (e.g. missing from App.OnStartup / #if GRAFT_TEST).")
            .AppendLine("- The app crashed during startup (missing dependency, unhandled exception, build failure for .csproj).");

        var tail = outputTail.Snapshot();
        if (tail.Length > 0)
        {
            message.AppendLine("Last output from the process:").Append(tail);
        }

        return message.ToString().TrimEnd();
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
