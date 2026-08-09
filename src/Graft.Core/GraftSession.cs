using System.Diagnostics;

namespace Graft.Core;

/// <summary>
/// A launched application session: pipe connection plus owned child process.
/// </summary>
/// <remarks>
/// Disposing the session closes the pipe and terminates the child process (default lifetime).
/// </remarks>
public sealed class GraftSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly AgentConnection _connection;
    private bool _disposed;

    internal GraftSession(Process process, AgentConnection connection)
    {
        _process = process;
        _connection = connection;
    }

    /// <summary>
    /// Gets the handshaken agent connection for this session.
    /// </summary>
    public AgentConnection Connection => _connection;

    /// <summary>
    /// Gets the child process id (0 if unavailable).
    /// </summary>
    public int ProcessId
    {
        get
        {
            try
            {
                return _process.Id;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            TryKill(_process);
            _process.Dispose();
        }
    }

    private static void TryKill(Process process)
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
            // Best-effort cleanup.
        }
    }
}
