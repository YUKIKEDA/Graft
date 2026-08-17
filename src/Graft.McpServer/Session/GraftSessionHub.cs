using Graft.Core;

namespace Graft.McpServer.Session;

/// <summary>
/// Holds at most one <see cref="GraftSession"/> for atomic MCP tools (stdio process lifetime).
/// </summary>
public sealed class GraftSessionHub : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private GraftSession? _session;
    private bool _disposed;

    /// <summary>
    /// Runs <paramref name="action"/> under the session lock.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="action">Work that may read or replace the current session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The action result.</returns>
    public async Task<T> RunAsync<T>(Func<GraftSession?, Task<T>> action, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(_session).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Replaces the current session (caller must dispose the previous one when needed).
    /// </summary>
    /// <param name="session">New session, or <see langword="null"/> to clear.</param>
    public void SetSession(GraftSession? session) => _session = session;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_session is not null)
            {
                await _session.DisposeAsync().ConfigureAwait(false);
                _session = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
