namespace Graft.Instrumentation;

/// <summary>
/// In-process agent entry point hosted inside the application under test.
/// </summary>
/// <remarks>
/// <see cref="Start"/> / <see cref="Stop"/> exist only when this assembly is compiled with
/// <c>GRAFT_TEST</c>. Call sites in consumer apps must also be gated with <c>#if GRAFT_TEST</c>.
/// </remarks>
public static class Agent
{
#if GRAFT_TEST
    private static readonly object Sync = new();

    /// <summary>
    /// Gets the active session when the agent has started; otherwise <see langword="null"/>.
    /// </summary>
    public static AgentSession? Current { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a session is active.
    /// </summary>
    public static bool IsRunning => Current is not null;

    /// <summary>
    /// Starts the agent when <c>GRAFT_ENABLE=1</c> and required environment variables are present.
    /// </summary>
    /// <remarks>
    /// Without <c>GRAFT_ENABLE=1</c> this method returns without starting.
    /// When enabled, listens on <c>GRAFT_PIPE_NAME</c> with same-user ACL and accepts a single
    /// client (reconnect after disconnect is allowed).
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when enabled but <c>GRAFT_PIPE_NAME</c> is missing or empty.
    /// </exception>
    public static void Start()
    {
        if (!GraftEnvironment.IsEnableFlagSet())
        {
            return;
        }

        var pipeName = Environment.GetEnvironmentVariable(GraftEnvironment.PipeName);
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new InvalidOperationException(
                $"{GraftEnvironment.PipeName} is required when {GraftEnvironment.Enable}=1."
            );
        }

        var token =
            Environment.GetEnvironmentVariable(GraftEnvironment.ConnectToken) ?? string.Empty;

        lock (Sync)
        {
            Current?.Dispose();
            Current = new AgentSession(pipeName, token);
        }
    }

    /// <summary>
    /// Stops the agent, closes the pipe server, and clears <see cref="Current"/>.
    /// </summary>
    public static void Stop()
    {
        lock (Sync)
        {
            Current?.Dispose();
            Current = null;
        }
    }
#endif
}
