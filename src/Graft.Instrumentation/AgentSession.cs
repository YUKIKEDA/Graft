using Graft.Instrumentation.Pipe;

namespace Graft.Instrumentation;

#if GRAFT_TEST

/// <summary>
/// Runtime configuration and pipe server for an active agent session.
/// </summary>
public sealed class AgentSession : IDisposable
{
    private readonly AgentPipeServer _server;
    private bool _disposed;

    /// <summary>
    /// Initializes a new session and starts listening on the named pipe.
    /// </summary>
    /// <param name="pipeName">Named pipe name.</param>
    /// <param name="connectToken">Handshake token (must be non-empty).</param>
    /// <param name="backend">Framework services for this session. Null services fail their wire methods.</param>
    public AgentSession(string pipeName, string connectToken, AgentBackend? backend)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectToken);
        PipeName = pipeName;
        ConnectToken = connectToken;
        _server = new AgentPipeServer(PipeName, ConnectToken, backend);
    }

    /// <summary>
    /// Gets the named pipe name.
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// Gets the handshake token.
    /// </summary>
    public string ConnectToken { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _server.Dispose();
    }
}

#endif
