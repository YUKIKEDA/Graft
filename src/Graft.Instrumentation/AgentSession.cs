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
    /// <param name="connectToken">Handshake token (may be empty).</param>
    public AgentSession(string pipeName, string connectToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        PipeName = pipeName;
        ConnectToken = connectToken ?? string.Empty;
        _server = new AgentPipeServer(PipeName, ConnectToken);
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
