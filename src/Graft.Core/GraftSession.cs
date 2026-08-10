using System.Diagnostics;
using Graft.Core.Diagnostics;
using Graft.Core.Selectors;
using Graft.Protocol;
using Graft.Protocol.Messages;

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
    /// Lists open windows with session-local <c>windowId</c> values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Window list result.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task<ListWindowsResult> ListWindowsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await _connection
                .ListWindowsAsync(cancellationToken)
                .ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ListWindows, $"{result.Windows.Count} window(s)");
            return result;
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Switches the agent target window used by getTree / resolve / screenshot / actions.
    /// </summary>
    /// <param name="windowId">Session-local window id from <see cref="ListWindowsAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the switch succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SwitchToWindowAsync(
        int windowId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _connection.SwitchWindowAsync(windowId, cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.SwitchWindow, $"windowId={windowId}");
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Waits until a window matching <paramref name="title"/> and/or <paramref name="automationId"/> appears.
    /// </summary>
    /// <param name="title">Optional exact window title.</param>
    /// <param name="automationId">Optional exact window automation id.</param>
    /// <param name="switchTo">When true (default), switches the target to the matched window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched window descriptor.</returns>
    /// <exception cref="ArgumentException">Neither title nor automationId was provided.</exception>
    /// <exception cref="GraftException">Timed out or RPC failed.</exception>
    public async Task<WindowInfo> WaitForWindowAsync(
        string? title = null,
        string? automationId = null,
        bool switchTo = true,
        CancellationToken cancellationToken = default
    )
    {
        var hasTitle = !string.IsNullOrWhiteSpace(title);
        var hasAutomationId = !string.IsNullOrWhiteSpace(automationId);
        if (!hasTitle && !hasAutomationId)
        {
            throw new ArgumentException("At least one of title or automationId must be provided.");
        }

        var timeout = PositiveOrDefault(
            WaitOptions.ExpectTimeout,
            WaitOptions.DefaultExpectTimeout
        );
        var poll = PositiveOrDefault(WaitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var listed = await _connection
                .ListWindowsAsync(cancellationToken)
                .ConfigureAwait(false);
            var match = listed.Windows.FirstOrDefault(window =>
                (!hasTitle || string.Equals(window.Title, title, StringComparison.Ordinal))
                && (
                    !hasAutomationId
                    || string.Equals(window.AutomationId, automationId, StringComparison.Ordinal)
                )
            );

            if (match is not null)
            {
                if (switchTo)
                {
                    await _connection
                        .SwitchWindowAsync(match.WindowId, cancellationToken)
                        .ConfigureAwait(false);
                    _operationLog.Record(
                        FailureSteps.WaitForWindow,
                        $"windowId={match.WindowId};switched"
                    );
                }
                else
                {
                    _operationLog.Record(FailureSteps.WaitForWindow, $"windowId={match.WindowId}");
                }

                return match;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken)
                .ConfigureAwait(false);
        }

        var criteria =
            hasTitle && hasAutomationId ? $"title='{title}', automationId='{automationId}'"
            : hasTitle ? $"title='{title}'"
            : $"automationId='{automationId}'";

        throw new GraftException(
            GraftErrorCodes.ActionTimeout,
            $"Timed out after {timeout.TotalSeconds:0.###}s waiting for window ({criteria})."
        );
    }

    /// <summary>
    /// Arms the next <c>OpenFileDialog.ShowDialog</c> (via RunDialog seam) to return <paramref name="path"/> (one-shot).
    /// </summary>
    /// <param name="path">File path to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            await _connection.ArmOpenFileAsync(path, cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmOpenFile, path);
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Arms the next <c>OpenFileDialog.ShowDialog</c> (via RunDialog seam) as cancel (one-shot).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFileCancelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.ArmOpenFileCancelAsync(cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmOpenFileCancel, "cancel");
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Arms the next <c>SaveFileDialog.ShowDialog</c> (via RunDialog seam) to return <paramref name="path"/> (one-shot).
    /// </summary>
    /// <param name="path">File path to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmSaveFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            await _connection.ArmSaveFileAsync(path, cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmSaveFile, path);
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Arms the next <c>SaveFileDialog.ShowDialog</c> (via RunDialog seam) as cancel (one-shot).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmSaveFileCancelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.ArmSaveFileCancelAsync(cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmSaveFileCancel, "cancel");
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Arms the next <c>OpenFolderDialog.ShowDialog</c> (via RunDialog seam) to return <paramref name="path"/> (one-shot).
    /// </summary>
    /// <param name="path">Folder path to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            await _connection.ArmOpenFolderAsync(path, cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmOpenFolder, path);
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Arms the next <c>OpenFolderDialog.ShowDialog</c> (via RunDialog seam) as cancel (one-shot).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFolderCancelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.ArmOpenFolderCancelAsync(cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmOpenFolderCancel, "cancel");
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Arms the next <c>MessageBox.Show</c> (via seam) to return <paramref name="result"/> (one-shot).
    /// </summary>
    /// <param name="result">MessageBoxResult name: None, OK, Cancel, Yes, or No.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmMessageBoxAsync(
        string result,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        try
        {
            await _connection.ArmMessageBoxAsync(result, cancellationToken).ConfigureAwait(false);
            _operationLog.Record(FailureSteps.ArmMessageBox, result);
        }
        catch (GraftException)
        {
            throw;
        }
    }

    /// <summary>
    /// Captures a PNG screenshot of the current target window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot meta and PNG bytes.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task<Screenshot> ScreenshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (meta, pngBytes) = await _connection
                .ScreenshotAsync(cancellationToken)
                .ConfigureAwait(false);
            var shot = new Screenshot(meta.Format, meta.Width, meta.Height, pngBytes);
            _operationLog.Record(
                FailureSteps.Screenshot,
                $"{shot.Width}x{shot.Height}:{shot.PngBytes.Length}"
            );
            return shot;
        }
        catch (GraftException)
        {
            throw;
        }
    }

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

    private static TimeSpan PositiveOrDefault(TimeSpan value, TimeSpan fallback) =>
        value <= TimeSpan.Zero ? fallback : value;

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
