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
    private readonly OperationTimeline? _timeline;
    private bool _disposed;

    internal GraftSession(Process process, AgentConnection connection, TimelineOptions? timeline = null)
    {
        _process = process;
        _connection = connection;
        if (timeline is not null)
        {
            _timeline = new OperationTimeline(
                timeline,
                async ct =>
                {
                    var (_, pngBytes) = await _connection.ScreenshotAsync(ct).ConfigureAwait(false);
                    return pngBytes;
                }
            );
        }
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
    /// Gets the path to the timeline <c>index.html</c> after save/dispose finalize, when kept.
    /// </summary>
    public string? TimelineIndexPath => _timeline?.IndexPath;

    /// <summary>
    /// Creates an element query for the given selector (resolved via getTree scoring).
    /// </summary>
    /// <param name="selector">Composite selector.</param>
    /// <returns>A query that can invoke or expect against the live tree.</returns>
    public ElementQuery GetBy(Selector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new ElementQuery(_connection, selector, WaitOptions, _operationLog, timeline: _timeline);
    }

    /// <summary>
    /// Creates an element query for <paramref name="automationId"/>.
    /// </summary>
    /// <param name="automationId">Automation id shorthand.</param>
    /// <returns>A query that can invoke or expect against the live tree.</returns>
    public ElementQuery GetByAutomationId(string automationId) => GetBy(Selector.ByAutomationId(automationId));

    /// <summary>
    /// Creates an element query for an exact automation / display name.
    /// </summary>
    /// <param name="name">Name criterion (hard match).</param>
    /// <returns>A query that can invoke or expect against the live tree.</returns>
    public ElementQuery GetByName(string name) => GetBy(Selector.ByName(name));

    /// <summary>
    /// Creates an element query for an exact control type label.
    /// </summary>
    /// <param name="controlType">Control type (e.g. <c>Button</c>).</param>
    /// <returns>A query that can invoke or expect against the live tree.</returns>
    public ElementQuery GetByControlType(string controlType) => GetBy(Selector.ByControlType(controlType));

    /// <summary>
    /// Lists open windows with session-local <c>windowId</c> values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Window list result.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task<ListWindowsResult> ListWindowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _connection.ListWindowsAsync(cancellationToken).ConfigureAwait(false);
            await RecordSuccessAsync(FailureSteps.ListWindows, $"{result.Windows.Count} window(s)", cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
    public async Task SwitchToWindowAsync(int windowId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.SwitchWindowAsync(windowId, cancellationToken).ConfigureAwait(false);
            await RecordSuccessAsync(FailureSteps.SwitchWindow, $"windowId={windowId}", cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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

        var timeout = PositiveOrDefault(WaitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        var poll = PositiveOrDefault(WaitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var listed = await _connection.ListWindowsAsync(cancellationToken).ConfigureAwait(false);
            var match = listed.Windows.FirstOrDefault(window =>
                (!hasTitle || string.Equals(window.Title, title, StringComparison.Ordinal))
                && (!hasAutomationId || string.Equals(window.AutomationId, automationId, StringComparison.Ordinal))
            );

            if (match is not null)
            {
                if (switchTo)
                {
                    await _connection.SwitchWindowAsync(match.WindowId, cancellationToken).ConfigureAwait(false);
                    await RecordSuccessAsync(FailureSteps.WaitForWindow, $"windowId={match.WindowId};switched", cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await RecordSuccessAsync(FailureSteps.WaitForWindow, $"windowId={match.WindowId}", cancellationToken).ConfigureAwait(false);
                }

                return match;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken).ConfigureAwait(false);
        }

        var criteria =
            hasTitle && hasAutomationId ? $"title='{title}', automationId='{automationId}'"
            : hasTitle ? $"title='{title}'"
            : $"automationId='{automationId}'";

        _timeline?.MarkFailed();
        throw new GraftException(GraftErrorCodes.ActionTimeout, $"Timed out after {timeout.TotalSeconds:0.###}s waiting for window ({criteria}).");
    }

    /// <summary>
    /// Waits until no window matches <paramref name="title"/> and/or <paramref name="automationId"/>.
    /// </summary>
    /// <param name="title">Optional exact window title.</param>
    /// <param name="automationId">Optional exact window automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the window is no longer listed.</returns>
    /// <exception cref="ArgumentException">Neither title nor automationId was provided.</exception>
    /// <exception cref="GraftException">Timed out or RPC failed.</exception>
    public async Task WaitForWindowClosedAsync(string? title = null, string? automationId = null, CancellationToken cancellationToken = default)
    {
        var hasTitle = !string.IsNullOrWhiteSpace(title);
        var hasAutomationId = !string.IsNullOrWhiteSpace(automationId);
        if (!hasTitle && !hasAutomationId)
        {
            throw new ArgumentException("At least one of title or automationId must be provided.");
        }

        var timeout = PositiveOrDefault(WaitOptions.ExpectTimeout, WaitOptions.DefaultExpectTimeout);
        var poll = PositiveOrDefault(WaitOptions.PollInterval, WaitOptions.DefaultPollInterval);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var listed = await _connection.ListWindowsAsync(cancellationToken).ConfigureAwait(false);
            var match = listed.Windows.FirstOrDefault(window =>
                (!hasTitle || string.Equals(window.Title, title, StringComparison.Ordinal))
                && (!hasAutomationId || string.Equals(window.AutomationId, automationId, StringComparison.Ordinal))
            );

            if (match is null)
            {
                var detail =
                    hasTitle && hasAutomationId ? $"title='{title}', automationId='{automationId}'"
                    : hasTitle ? $"title='{title}'"
                    : $"automationId='{automationId}'";
                await RecordSuccessAsync(FailureSteps.WaitForWindowClosed, detail, cancellationToken).ConfigureAwait(false);
                return;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < poll ? remaining : poll, cancellationToken).ConfigureAwait(false);
        }

        var criteria =
            hasTitle && hasAutomationId ? $"title='{title}', automationId='{automationId}'"
            : hasTitle ? $"title='{title}'"
            : $"automationId='{automationId}'";

        _timeline?.MarkFailed();
        throw new GraftException(
            GraftErrorCodes.ActionTimeout,
            $"Timed out after {timeout.TotalSeconds:0.###}s waiting for window to close ({criteria})."
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
            await RecordSuccessAsync(FailureSteps.ArmOpenFile, path, cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
            await RecordSuccessAsync(FailureSteps.ArmOpenFileCancel, "cancel", cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
            await RecordSuccessAsync(FailureSteps.ArmSaveFile, path, cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
            await RecordSuccessAsync(FailureSteps.ArmSaveFileCancel, "cancel", cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
            await RecordSuccessAsync(FailureSteps.ArmOpenFolder, path, cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
            await RecordSuccessAsync(FailureSteps.ArmOpenFolderCancel, "cancel", cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
    public async Task ArmMessageBoxAsync(string result, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        try
        {
            await _connection.ArmMessageBoxAsync(result, cancellationToken).ConfigureAwait(false);
            await RecordSuccessAsync(FailureSteps.ArmMessageBox, result, cancellationToken).ConfigureAwait(false);
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
            throw;
        }
    }

    /// <summary>
    /// Captures a PNG screenshot of the current target window.
    /// Open ToolTips, Popups, and ContextMenus are composited in screen space.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot meta and PNG bytes.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task<Screenshot> ScreenshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (meta, pngBytes) = await _connection.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
            var shot = new Screenshot(meta.Format, meta.Width, meta.Height, pngBytes);
            await RecordSuccessAsync(FailureSteps.Screenshot, $"{shot.Width}x{shot.Height}:{shot.PngBytes.Length}", cancellationToken, shot.PngBytes)
                .ConfigureAwait(false);
            return shot;
        }
        catch (GraftException)
        {
            _timeline?.MarkFailed();
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
            _ = _timeline?.FinalizeArtifacts();
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            TryKill(_process);
            _process.Dispose();
        }
    }

    /// <summary>
    /// Finalizes timeline artifacts now (idempotent). Also runs automatically on dispose.
    /// </summary>
    /// <returns>Path to index.html when kept; otherwise null.</returns>
    public string? SaveTimeline()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _timeline?.FinalizeArtifacts();
    }

    private async Task RecordSuccessAsync(string action, string? detail, CancellationToken cancellationToken, byte[]? pngBytes = null)
    {
        _operationLog.Record(action, detail);
        if (_timeline is not null)
        {
            await _timeline.CaptureAfterAsync(action, detail, cancellationToken, pngBytes).ConfigureAwait(false);
        }
    }

    private static TimeSpan PositiveOrDefault(TimeSpan value, TimeSpan fallback) => value <= TimeSpan.Zero ? fallback : value;

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
