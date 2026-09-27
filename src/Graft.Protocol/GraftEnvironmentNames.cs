namespace Graft.Protocol;

/// <summary>
/// Shared process settings: agent environment variable names and the MSBuild configuration that enables the agent.
/// </summary>
public static class GraftEnvironmentNames
{
    /// <summary>
    /// Environment variable that must equal <c>1</c> before the agent opens a pipe.
    /// </summary>
    public const string Enable = "GRAFT_ENABLE";

    /// <summary>
    /// Environment variable that carries the named pipe name.
    /// </summary>
    public const string PipeName = "GRAFT_PIPE_NAME";

    /// <summary>
    /// Environment variable that carries the handshake token.
    /// </summary>
    public const string ConnectToken = "GRAFT_CONNECT_TOKEN";

    /// <summary>
    /// MSBuild configuration that defines <c>GRAFT_TEST</c>.
    /// </summary>
    public const string TestConfiguration = "GraftTest";
}
