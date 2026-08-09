namespace Graft.Instrumentation;

#if GRAFT_TEST

/// <summary>
/// Runtime configuration captured when the agent starts successfully.
/// </summary>
public sealed class AgentSession
{
    /// <summary>
    /// Initializes a new session.
    /// </summary>
    /// <param name="pipeName">Named pipe name.</param>
    /// <param name="connectToken">Handshake token (may be empty).</param>
    public AgentSession(string pipeName, string connectToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        PipeName = pipeName;
        ConnectToken = connectToken ?? string.Empty;
    }

    /// <summary>
    /// Gets the named pipe name.
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// Gets the handshake token.
    /// </summary>
    public string ConnectToken { get; }
}

#endif
