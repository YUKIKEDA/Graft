namespace Graft.Instrumentation;

/// <summary>
/// Environment variable names used to enable and configure the in-process agent.
/// </summary>
public static class GraftEnvironment
{
    /// <summary>
    /// When set to <c>1</c>, <see cref="Agent.Start"/> may activate the agent.
    /// </summary>
    public const string Enable = "GRAFT_ENABLE";

    /// <summary>
    /// Named pipe name the agent listens on.
    /// </summary>
    public const string PipeName = "GRAFT_PIPE_NAME";

    /// <summary>
    /// Shared secret presented during handshake.
    /// </summary>
    public const string ConnectToken = "GRAFT_CONNECT_TOKEN";

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="Enable"/> equals <c>1</c>.
    /// </summary>
    /// <returns><see langword="true"/> if the enable flag is set.</returns>
    public static bool IsEnableFlagSet() =>
        string.Equals(Environment.GetEnvironmentVariable(Enable), "1", StringComparison.Ordinal);
}
