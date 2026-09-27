#if GRAFT_TEST
using Graft.Instrumentation.Dialogs;
#endif

namespace Graft.Instrumentation;

/// <summary>
/// In-process agent entry point hosted inside the application under test.
/// </summary>
/// <remarks>
/// <c>Start</c> / <c>Stop</c> exist only when this assembly is compiled with
/// <c>GRAFT_TEST</c>. Call sites in consumer apps must also be gated with <c>#if GRAFT_TEST</c>.
/// </remarks>
public static class Agent
{
#if GRAFT_TEST
    private static readonly object Sync = new();

    private static AgentBackend? _backend;

    /// <summary>
    /// Gets the active session when the agent has started; otherwise <see langword="null"/>.
    /// </summary>
    public static AgentSession? Current { get; private set; }

    /// <summary>
    /// Gets the backend passed to <see cref="Use"/>, or <see langword="null"/> when none is registered.
    /// </summary>
    public static AgentBackend? Backend => _backend;

    /// <summary>
    /// Gets a value indicating whether a session is active.
    /// </summary>
    public static bool IsRunning => Current is not null;

    /// <summary>
    /// Registers the framework backend used by the next <see cref="Start"/>.
    /// </summary>
    /// <param name="backend">Backend instance, or <see langword="null"/> to clear it.</param>
    /// <remarks>
    /// Call this before <see cref="Start"/>. The pipe server keeps the instance it was given.
    /// </remarks>
    public static void Use(AgentBackend? backend)
    {
        lock (Sync)
        {
            _backend = backend;
        }
    }

    /// <summary>
    /// Clears <see cref="Backend"/> and the dialog arms.
    /// </summary>
    /// <remarks>
    /// Tests call this so one process-wide agent does not keep the previous backend or a pending dialog arm.
    /// </remarks>
    public static void Reset()
    {
        lock (Sync)
        {
            _backend = null;
        }

        DialogArm.OpenFile.Reset();
        DialogArm.SaveFile.Reset();
        DialogArm.OpenFolder.Reset();
        MessageBoxArm.Reset();
    }

    /// <summary>
    /// Starts the agent when <c>GRAFT_ENABLE=1</c> and required environment variables are present.
    /// </summary>
    /// <remarks>
    /// Without <c>GRAFT_ENABLE=1</c> this method returns without starting.
    /// When enabled, listens on <c>GRAFT_PIPE_NAME</c> with same-user ACL and accepts a single
    /// client (reconnect after disconnect is allowed).
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when enabled but <c>GRAFT_PIPE_NAME</c> or <c>GRAFT_CONNECT_TOKEN</c> is missing or empty.
    /// An empty token would let any same-user process pass the handshake, so it is rejected.
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
            throw new InvalidOperationException($"{GraftEnvironment.PipeName} is required when {GraftEnvironment.Enable}=1.");
        }

        var token = Environment.GetEnvironmentVariable(GraftEnvironment.ConnectToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException($"{GraftEnvironment.ConnectToken} is required when {GraftEnvironment.Enable}=1.");
        }

        lock (Sync)
        {
            Current?.Dispose();
            Current = new AgentSession(pipeName, token, _backend);
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
