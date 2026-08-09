using System.Diagnostics;
using Graft.Core.Diagnostics;
using Graft.Core.Selectors;

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
    private readonly OperationLog _operationLog = new();
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
    /// Gets or sets wait / expect timeouts used by <see cref="GetBy"/>.
    /// </summary>
    public WaitOptions WaitOptions { get; set; } = new();

    /// <summary>
    /// Creates an element query for the given selector (resolved via getTree scoring).
    /// </summary>
    /// <param name="selector">Composite selector.</param>
    /// <returns>A query that can invoke or expect against the live tree.</returns>
    public ElementQuery GetBy(Selector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new ElementQuery(_connection, selector, WaitOptions, _operationLog);
    }

    /// <summary>
    /// Creates an element query for <paramref name="automationId"/>.
    /// </summary>
    /// <param name="automationId">Automation id shorthand.</param>
    /// <returns>A query that can invoke or expect against the live tree.</returns>
    public ElementQuery GetByAutomationId(string automationId) =>
        GetBy(Selector.ByAutomationId(automationId));

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
