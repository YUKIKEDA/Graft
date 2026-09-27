using Graft.Protocol;

namespace Graft.Instrumentation;

/// <summary>
/// Environment variable names used to enable and configure the in-process agent.
/// </summary>
public static class GraftEnvironment
{
    /// <summary>
    /// When set to <c>1</c>, <c>Agent.Start</c> may activate the agent.
    /// </summary>
    public const string Enable = GraftEnvironmentNames.Enable;

    /// <summary>
    /// Named pipe name the agent listens on.
    /// </summary>
    public const string PipeName = GraftEnvironmentNames.PipeName;

    /// <summary>
    /// Shared secret presented during handshake.
    /// </summary>
    public const string ConnectToken = GraftEnvironmentNames.ConnectToken;

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="Enable"/> equals <c>1</c>.
    /// </summary>
    /// <returns><see langword="true"/> if the enable flag is set.</returns>
    public static bool IsEnableFlagSet() => string.Equals(Environment.GetEnvironmentVariable(Enable), "1", StringComparison.Ordinal);
}
