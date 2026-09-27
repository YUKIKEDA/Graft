using System.IO.Pipes;
using System.Text.Json;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Core;

/// <summary>
/// Low-level named-pipe session to an instrumented agent (Connect + Handshake + RPC).
/// </summary>
/// <remarks>
/// Prefer <c>Application.LaunchAsync</c> (M2 Batch 2+) for the documented main path.
/// This type is the Connect / wire surface used by Launch and advanced callers.
/// </remarks>
public sealed class AgentConnection : IAsyncDisposable
{
    private readonly NamedPipeClientStream _stream;

    // Serializes each write-request / read-response exchange so concurrent callers cannot interleave frames.
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    // Requests whose response was never read because the caller cancelled after the write.
    // Value: whether an OK response is followed by a binary frame (screenshot). Guarded by _ioLock.
    private readonly Dictionary<string, bool> _abandoned = new(StringComparer.Ordinal);
    private int _pendingBinaryFrames;

    // A read started with CancellationToken.None and parked when the caller cancelled.
    // The next send awaits this instead of starting a second read on the same pipe.
    // Guarded by _ioLock. Never observe a cancelled Stream.ReadAsync: that leaves the
    // operation running and the following read waits forever.
    private Task<ResponseMessage>? _parkedResponse;
    private bool _broken;
    private int _nextId = 1;
    private bool _disposed;

    private AgentConnection(NamedPipeClientStream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Connects to the agent pipe and completes Handshake.
    /// </summary>
    /// <param name="pipeName">Named pipe name (<c>GRAFT_PIPE_NAME</c>).</param>
    /// <param name="token">Connect token (<c>GRAFT_CONNECT_TOKEN</c>).</param>
    /// <param name="timeout">Overall connect + handshake budget (both phases share this).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open, handshaken connection.</returns>
    /// <exception cref="GraftException">Connection, handshake, or overall timeout failed.</exception>
    public static async Task<AgentConnection> ConnectAsync(
        string pipeName,
        string token,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var budgetToken = timeoutCts.Token;

        AgentConnection? connection = null;
        try
        {
            var stream = await ConnectPipeAsync(pipeName, budgetToken).ConfigureAwait(false);
            connection = new AgentConnection(stream);
            await connection.HandshakeAsync(token, budgetToken).ConfigureAwait(false);
            var result = connection;
            connection = null;
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GraftException(GraftErrorCodes.ActionTimeout, $"Connect + handshake timed out after {timeout.TotalSeconds:0.###}s.");
        }
        finally
        {
            if (connection is not null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Calls <c>getTree</c> with default depth / maxNodes limits.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tree result from the agent.</returns>
    /// <exception cref="GraftException">RPC failed or result missing.</exception>
    public async Task<GetTreeResult> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.GetTree,
                    Params = JsonMessageCodec.SerializeParams(new GetTreeParams { Depth = 25, MaxNodes = 2000 }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "getTree failed.");
        if (response.Result is not { } resultElement)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "getTree returned no result.");
        }

        return resultElement.Deserialize<GetTreeResult>(JsonMessageCodec.Options)
            ?? throw new GraftException(GraftErrorCodes.ActionFailed, "getTree result deserialized to null.");
    }

    /// <summary>
    /// Calls <c>invoke</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when invoke succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task InvokeAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Invoke,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "invoke failed.");
    }

    /// <summary>
    /// Calls <c>rightClick</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when rightClick succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task RightClickAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.RightClick,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "rightClick failed.");
    }

    /// <summary>
    /// Calls <c>doubleClick</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when doubleClick succeeds.</returns>
    public async Task DoubleClickAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.DoubleClick,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "doubleClick failed.");
    }

    /// <summary>
    /// Calls <c>hover</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when hover succeeds.</returns>
    public async Task HoverAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Hover,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "hover failed.");
    }

    /// <summary>
    /// Calls <c>drag</c> from one automation id to another.
    /// </summary>
    /// <param name="automationId">Source automation id.</param>
    /// <param name="toAutomationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when drag succeeds.</returns>
    public async Task DragAsync(string automationId, string toAutomationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toAutomationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Drag,
                    Params = JsonMessageCodec.SerializeParams(new DragParams { AutomationId = automationId, ToAutomationId = toAutomationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "drag failed.");
    }

    /// <summary>
    /// Calls <c>clickAt</c> with DIP offsets from the element's clickable point.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="offsetX">Horizontal DIP offset.</param>
    /// <param name="offsetY">Vertical DIP offset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when clickAt succeeds.</returns>
    public async Task ClickAtAsync(string automationId, double offsetX, double offsetY, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ClickAt,
                    Params = JsonMessageCodec.SerializeParams(
                        new ClickAtParams
                        {
                            AutomationId = automationId,
                            OffsetX = offsetX,
                            OffsetY = offsetY,
                        }
                    ),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "clickAt failed.");
    }

    /// <summary>
    /// Calls <c>wheel</c> over the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="delta">Wheel delta (typically multiples of 120).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when wheel succeeds.</returns>
    public async Task WheelAsync(string automationId, int delta, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Wheel,
                    Params = JsonMessageCodec.SerializeParams(new WheelParams { AutomationId = automationId, Delta = delta }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "wheel failed.");
    }

    /// <summary>
    /// Calls <c>setValue</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="value">Replacement text (empty string clears).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SetValueAsync(string automationId, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SetValue,
                    Params = JsonMessageCodec.SerializeParams(new SetValueParams { AutomationId = automationId, Value = value }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "setValue failed.");
    }

    /// <summary>
    /// Calls <c>toggle</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when toggle succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ToggleAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Toggle,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "toggle failed.");
    }

    /// <summary>
    /// Calls <c>sendKeys</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="text">Literal text to type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when sendKeys succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SendKeysAsync(string automationId, string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SendKeys,
                    Params = JsonMessageCodec.SerializeParams(new SendKeysParams { AutomationId = automationId, Text = text }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "sendKeys failed.");
    }

    /// <summary>
    /// Calls <c>typeHuman</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="text">Literal text to type.</param>
    /// <param name="delayMs">Milliseconds to wait between Unicode scalars.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when typeHuman succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task TypeHumanAsync(string automationId, string text, int delayMs, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(delayMs);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.TypeHuman,
                    Params = JsonMessageCodec.SerializeParams(
                        new TypeHumanParams
                        {
                            AutomationId = automationId,
                            Text = text,
                            DelayMs = delayMs,
                        }
                    ),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "typeHuman failed.");
    }

    /// <summary>
    /// Calls <c>pressKeys</c> for one keyboard chord on an element.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="keys">Chord DSL (e.g. <c>Control+A</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when pressKeys succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task PressKeysAsync(string automationId, string keys, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(keys);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.PressKeys,
                    Params = JsonMessageCodec.SerializeParams(new PressKeysParams { AutomationId = automationId, Keys = keys }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "pressKeys failed.");
    }

    /// <summary>
    /// Calls <c>scrollIntoView</c> and returns the realized element identity.
    /// </summary>
    /// <param name="automationId">Target element or list automation id.</param>
    /// <param name="index">Optional list item index (virtualized lists).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identity of the scrolled element.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task<ElementIdentity> ScrollIntoViewAsync(string automationId, int? index = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ScrollIntoView,
                    Params = JsonMessageCodec.SerializeParams(new ScrollIntoViewParams { AutomationId = automationId, Index = index }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "scrollIntoView failed.");
        if (response.Result is not { } resultElement)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "scrollIntoView returned no result.");
        }

        return resultElement.Deserialize<ElementIdentity>(JsonMessageCodec.Options)
            ?? throw new GraftException(GraftErrorCodes.ActionFailed, "scrollIntoView result deserialized to null.");
    }

    /// <summary>
    /// Calls <c>select</c> for a list/combo item by index.
    /// </summary>
    /// <param name="automationId">List or combo automation id.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SelectAsync(string automationId, int index, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Select,
                    Params = JsonMessageCodec.SerializeParams(new SelectParams { AutomationId = automationId, Index = index }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "select failed.");
    }

    /// <summary>
    /// Calls <c>select</c> with an item name key.
    /// </summary>
    /// <param name="automationId">List / combo / tab automation id.</param>
    /// <param name="key">Item display / automation name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when select succeeds.</returns>
    public async Task SelectByKeyAsync(string automationId, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Select,
                    Params = JsonMessageCodec.SerializeParams(new SelectParams { AutomationId = automationId, Key = key }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "select failed.");
    }

    /// <summary>
    /// Calls <c>selectTree</c> for a slash-separated AutomationId path under a TreeView.
    /// </summary>
    /// <param name="automationId">TreeView automation id.</param>
    /// <param name="path">Slash-separated AutomationId segments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectTree succeeds.</returns>
    public async Task SelectTreeAsync(string automationId, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SelectTree,
                    Params = JsonMessageCodec.SerializeParams(new ElementPathParams { AutomationId = automationId, Path = path }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "selectTree failed.");
    }

    /// <summary>
    /// Calls <c>selectMany</c> to replace ListBox or DataGrid multi-selection by indexes.
    /// </summary>
    /// <param name="automationId">ListBox or DataGrid automation id.</param>
    /// <param name="indexes">Zero-based item/row indexes (empty clears selection).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectMany succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SelectManyAsync(string automationId, IReadOnlyList<int> indexes, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(indexes);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SelectMany,
                    Params = JsonMessageCodec.SerializeParams(new SelectManyParams { AutomationId = automationId, Indexes = indexes }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "selectMany failed.");
    }

    /// <summary>
    /// Calls <c>selectMenu</c> for a slash-separated AutomationId path under a menu root.
    /// </summary>
    /// <param name="automationId">Menu or open ContextMenu automation id.</param>
    /// <param name="path">Slash-separated AutomationId segments (root not included).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectMenu succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SelectMenuAsync(string automationId, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SelectMenu,
                    Params = JsonMessageCodec.SerializeParams(new ElementPathParams { AutomationId = automationId, Path = path }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "selectMenu failed.");
    }

    /// <summary>
    /// Calls <c>getCellText</c> for a DataGrid cell by column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task<string> GetCellTextAsync(string automationId, int row, int column, CancellationToken cancellationToken = default) =>
        GetCellTextCoreAsync(automationId, row, column, columnKey: null, cancellationToken);

    /// <summary>
    /// Calls <c>getCellText</c> for a DataGrid cell by column Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task<string> GetCellTextAsync(string automationId, int row, string columnKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return GetCellTextCoreAsync(automationId, row, column: null, columnKey, cancellationToken);
    }

    /// <summary>
    /// Calls <c>setCellValue</c> for a DataGrid cell by column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SetCellValueAsync(string automationId, int row, int column, string value, CancellationToken cancellationToken = default) =>
        SetCellValueCoreAsync(automationId, row, column, columnKey: null, value, cancellationToken);

    /// <summary>
    /// Calls <c>setCellValue</c> for a DataGrid cell by column Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setCellValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public Task SetCellValueAsync(string automationId, int row, string columnKey, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return SetCellValueCoreAsync(automationId, row, column: null, columnKey, value, cancellationToken);
    }

    /// <summary>
    /// Calls <c>selectCell</c> by column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectCell succeeds.</returns>
    public Task SelectCellAsync(string automationId, int row, int column, CancellationToken cancellationToken = default) =>
        SelectCellCoreAsync(automationId, row, column, columnKey: null, cancellationToken);

    /// <summary>
    /// Calls <c>selectCell</c> by column Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectCell succeeds.</returns>
    public Task SelectCellAsync(string automationId, int row, string columnKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        return SelectCellCoreAsync(automationId, row, column: null, columnKey, cancellationToken);
    }

    /// <summary>
    /// Calls <c>selectRow</c> by column Header key and cell value.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Exact cell display text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when selectRow succeeds.</returns>
    public async Task SelectRowAsync(string automationId, string columnKey, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SelectRow,
                    Params = JsonMessageCodec.SerializeParams(
                        new SelectRowParams
                        {
                            AutomationId = automationId,
                            ColumnKey = columnKey,
                            Value = value,
                        }
                    ),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "selectRow failed.");
    }

    /// <summary>
    /// Calls <c>clickColumnHeader</c>.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when clickColumnHeader succeeds.</returns>
    public async Task ClickColumnHeaderAsync(string automationId, string columnKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ClickColumnHeader,
                    Params = JsonMessageCodec.SerializeParams(new ColumnKeyParams { AutomationId = automationId, ColumnKey = columnKey }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "clickColumnHeader failed.");
    }

    /// <summary>
    /// Calls <c>addRow</c>.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when addRow succeeds.</returns>
    public async Task AddRowAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.AddRow,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "addRow failed.");
    }

    /// <summary>
    /// Calls <c>deleteSelectedRows</c>.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deleteSelectedRows succeeds.</returns>
    public async Task DeleteSelectedRowsAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.DeleteSelectedRows,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "deleteSelectedRows failed.");
    }

    /// <summary>
    /// Calls <c>expand</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when expand succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ExpandAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Expand,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "expand failed.");
    }

    /// <summary>
    /// Calls <c>collapse</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when collapse succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task CollapseAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Collapse,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "collapse failed.");
    }

    /// <summary>
    /// Calls <c>listWindows</c> and returns session-local window descriptors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Open windows in the target process.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task<ListWindowsResult> ListWindowsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ListWindows,
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "listWindows failed.");
        if (response.Result is not { } resultElement)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "listWindows returned no result.");
        }

        return resultElement.Deserialize<ListWindowsResult>(JsonMessageCodec.Options)
            ?? throw new GraftException(GraftErrorCodes.ActionFailed, "listWindows result deserialized to null.");
    }

    /// <summary>
    /// Calls <c>switchWindow</c> to set the agent target window.
    /// </summary>
    /// <param name="windowId">Session-local window id from <see cref="ListWindowsAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the switch succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SwitchWindowAsync(int windowId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SwitchWindow,
                    Params = JsonMessageCodec.SerializeParams(new WindowIdParams { WindowId = windowId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "switchWindow failed.");
    }

    /// <summary>
    /// Calls <c>invokeOpeningWindow</c> (non-blocking UI invoke for dialogs that may open).
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the invoke is queued.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task InvokeOpeningWindowAsync(string automationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.InvokeOpeningWindow,
                    Params = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "invokeOpeningWindow failed.");
    }

    /// <summary>
    /// Calls <c>armOpenFile</c> to arm the next Graft OpenFile seam with a path (OK).
    /// </summary>
    /// <param name="path">File path to return from the seam.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmOpenFile,
                    Params = JsonMessageCodec.SerializeParams(new DialogPathParams { Path = path }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armOpenFile failed.");
    }

    /// <summary>
    /// Calls <c>armOpenFileCancel</c> to arm the next Graft OpenFile seam as cancel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFileCancelAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmOpenFileCancel,
                    Params = JsonMessageCodec.SerializeParams(new EmptyParams()),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armOpenFileCancel failed.");
    }

    /// <summary>
    /// Calls <c>armSaveFile</c> to arm the next Graft SaveFile seam with a path (OK).
    /// </summary>
    /// <param name="path">File path to return from the seam.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmSaveFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmSaveFile,
                    Params = JsonMessageCodec.SerializeParams(new DialogPathParams { Path = path }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armSaveFile failed.");
    }

    /// <summary>
    /// Calls <c>armSaveFileCancel</c> to arm the next Graft SaveFile seam as cancel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmSaveFileCancelAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmSaveFileCancel,
                    Params = JsonMessageCodec.SerializeParams(new EmptyParams()),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armSaveFileCancel failed.");
    }

    /// <summary>
    /// Calls <c>armOpenFolder</c> to arm the next Graft OpenFolder seam with a path (OK).
    /// </summary>
    /// <param name="path">Folder path to return from the seam.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmOpenFolder,
                    Params = JsonMessageCodec.SerializeParams(new DialogPathParams { Path = path }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armOpenFolder failed.");
    }

    /// <summary>
    /// Calls <c>armOpenFolderCancel</c> to arm the next Graft OpenFolder seam as cancel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmOpenFolderCancelAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmOpenFolderCancel,
                    Params = JsonMessageCodec.SerializeParams(new EmptyParams()),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armOpenFolderCancel failed.");
    }

    /// <summary>
    /// Calls <c>armMessageBox</c> to arm the next MessageBox.Show with a result (OK).
    /// </summary>
    /// <param name="result">MessageBoxResult name: None, OK, Cancel, Yes, or No.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when arming succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ArmMessageBoxAsync(string result, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.ArmMessageBox,
                    Params = JsonMessageCodec.SerializeParams(new MessageBoxArmParams { Result = result }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "armMessageBox failed.");
    }

    /// <summary>
    /// Calls <c>screenshot</c> and reads the following raw PNG frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot meta and PNG bytes.</returns>
    /// <exception cref="GraftException">RPC failed or frame mismatch.</exception>
    public Task<(ScreenshotResult Meta, byte[] PngBytes)> ScreenshotAsync(CancellationToken cancellationToken = default) =>
        ScreenshotAsync(automationId: null, runtimeId: null, cancellationToken);

    /// <summary>
    /// Calls <c>screenshot</c> for the target window, or an element clip when a selector is given.
    /// </summary>
    /// <param name="automationId">Optional automation id to clip.</param>
    /// <param name="runtimeId">Optional runtime id to clip (used when automation id is empty).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot meta and PNG bytes.</returns>
    /// <exception cref="GraftException">RPC failed or frame mismatch.</exception>
    public async Task<(ScreenshotResult Meta, byte[] PngBytes)> ScreenshotAsync(
        string? automationId,
        int? runtimeId,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        JsonElement? paramsElement = null;
        if (!string.IsNullOrWhiteSpace(automationId) || runtimeId is not null)
        {
            paramsElement = JsonMessageCodec.SerializeParams(new ElementTargetParams { AutomationId = automationId, RuntimeId = runtimeId });
        }

        var (response, binary) = await SendCoreAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Screenshot,
                    Params = paramsElement,
                },
                expectsBinaryFollowUp: true,
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "screenshot failed.");
        if (response.Result is not { } resultElement)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "screenshot returned no result.");
        }

        var meta =
            resultElement.Deserialize<ScreenshotResult>(JsonMessageCodec.Options)
            ?? throw new GraftException(GraftErrorCodes.ActionFailed, "screenshot result deserialized to null.");

        var pngBytes = binary ?? Array.Empty<byte>();
        if (pngBytes.Length != meta.ByteLength)
        {
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                $"screenshot byteLength mismatch: meta={meta.ByteLength}, frame={pngBytes.Length}."
            );
        }

        return (meta, pngBytes);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_parkedResponse is { } parked)
        {
            _parkedResponse = null;
            _ = parked.ContinueWith(
                static task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
        }

        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    private async Task<string> GetCellTextCoreAsync(string automationId, int row, int? column, string? columnKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.GetCellText,
                    Params = JsonMessageCodec.SerializeParams(
                        new CellParams
                        {
                            AutomationId = automationId,
                            Row = row,
                            Column = column,
                            ColumnKey = columnKey,
                        }
                    ),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "getCellText failed.");
        if (response.Result is not { } resultElement)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "getCellText returned no result.");
        }

        var result =
            resultElement.Deserialize<CellTextResult>(JsonMessageCodec.Options)
            ?? throw new GraftException(GraftErrorCodes.ActionFailed, "getCellText result deserialized to null.");
        return result.Text;
    }

    private async Task SetCellValueCoreAsync(
        string automationId,
        int row,
        int? column,
        string? columnKey,
        string value,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SetCellValue,
                    Params = JsonMessageCodec.SerializeParams(
                        new CellParams
                        {
                            AutomationId = automationId,
                            Row = row,
                            Column = column,
                            ColumnKey = columnKey,
                            Value = value,
                        }
                    ),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "setCellValue failed.");
    }

    private async Task SelectCellCoreAsync(string automationId, int row, int? column, string? columnKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SelectCell,
                    Params = JsonMessageCodec.SerializeParams(
                        new CellParams
                        {
                            AutomationId = automationId,
                            Row = row,
                            Column = column,
                            ColumnKey = columnKey,
                        }
                    ),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "selectCell failed.");
    }

    private async Task HandshakeAsync(string token, CancellationToken cancellationToken)
    {
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Handshake,
                    Params = JsonMessageCodec.SerializeParams(new HandshakeParams { Token = token }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!response.Ok && response.Error?.Code == GraftErrorCodes.ProtocolVersionMismatch)
        {
            // Name both sides so the user knows which package to update.
            throw new GraftException(
                GraftErrorCodes.ProtocolVersionMismatch,
                $"{response.Error.Message} (This client is Graft.Core {CoreVersion}, protocol v={ProtocolVersion.Current}.)"
            );
        }

        EnsureOk(response, "Handshake failed.");
    }

    private static string CoreVersion =>
        typeof(AgentConnection)
            .Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion
        ?? typeof(AgentConnection).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    private static async Task<NamedPipeClientStream> ConnectPipeAsync(string pipeName, CancellationToken cancellationToken)
    {
        var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await stream.ConnectAsync(200, cancellationToken).ConfigureAwait(false);
                    return stream;
                }
                catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        var (response, _) = await SendCoreAsync(request, expectsBinaryFollowUp: false, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Writes one request and reads its correlated response (plus the binary follow-up frame for an OK
    /// screenshot) while holding the I/O lock.
    /// </summary>
    /// <remarks>
    /// Responses to requests abandoned by an earlier cancellation are drained and discarded. Any other
    /// id mismatch or malformed frame means the stream can no longer be trusted, so the connection is
    /// marked broken and every later call fails fast with <c>pipe.disconnected</c>.
    /// </remarks>
    private async Task<(ResponseMessage Response, byte[]? Binary)> SendCoreAsync(
        RequestMessage request,
        bool expectsBinaryFollowUp,
        CancellationToken cancellationToken
    )
    {
        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_broken)
            {
                throw new GraftException(
                    GraftErrorCodes.PipeDisconnected,
                    "Named pipe connection is unusable after an earlier protocol error. Relaunch or reconnect."
                );
            }

            var written = false;
            ResponseMessage? response = null;
            try
            {
                await JsonMessageCodec.WriteRequestAsync(_stream, request, cancellationToken).ConfigureAwait(false);
                written = true;

                while (_pendingBinaryFrames > 0)
                {
                    _ = await FrameIO.ReadAsync(_stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                    _pendingBinaryFrames--;
                }

                while (true)
                {
                    var candidate = await ReadResponseOrParkAsync(cancellationToken).ConfigureAwait(false);
                    if (string.Equals(candidate.Id, request.Id, StringComparison.Ordinal))
                    {
                        response = candidate;
                        break;
                    }

                    if (candidate.Id is not null && _abandoned.Remove(candidate.Id, out var staleHasBinary))
                    {
                        if (staleHasBinary && candidate.Ok)
                        {
                            _ = await FrameIO.ReadAsync(_stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }

                        continue;
                    }

                    _broken = true;
                    throw new GraftException(
                        GraftErrorCodes.PipeDisconnected,
                        $"Response id '{candidate.Id}' does not match request id '{request.Id}' ({request.Method}). "
                            + "The connection was marked unusable."
                    );
                }

                byte[]? binary = null;
                if (expectsBinaryFollowUp && response.Ok)
                {
                    binary = await FrameIO.ReadAsync(_stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return (response, binary);
            }
            catch (OperationCanceledException) when (written)
            {
                if (response is null)
                {
                    _abandoned[request.Id] = expectsBinaryFollowUp;
                }
                else if (expectsBinaryFollowUp && response.Ok)
                {
                    _pendingBinaryFrames++;
                }

                throw;
            }
            catch (IOException ex)
            {
                _broken = true;
                throw new GraftException(GraftErrorCodes.PipeDisconnected, "Named pipe connection was lost.", ex);
            }
            catch (ObjectDisposedException ex)
            {
                _broken = true;
                throw new GraftException(GraftErrorCodes.PipeDisconnected, "Named pipe connection was disposed.", ex);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                _broken = true;
                throw new GraftException(GraftErrorCodes.PipeDisconnected, $"Received a malformed response frame: {ex.Message}", ex);
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Reads the next response without cancelling the pipe read itself.
    /// </summary>
    /// <remarks>
    /// <see cref="Stream.ReadAsync(Memory{byte}, CancellationToken)"/> can throw while the read
    /// stays in flight. A later read on the same pipe then waits forever. On caller cancellation
    /// the in-flight read is parked for the next <see cref="SendCoreAsync"/>, which discards it
    /// via <see cref="_abandoned"/>.
    /// </remarks>
    private async Task<ResponseMessage> ReadResponseOrParkAsync(CancellationToken cancellationToken)
    {
        var readTask = _parkedResponse ?? JsonMessageCodec.ReadResponseAsync(_stream, CancellationToken.None);
        _parkedResponse = null;

        if (!cancellationToken.CanBeCanceled)
        {
            return await readTask.ConfigureAwait(false);
        }

        using var race = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancelTask = Task.Delay(Timeout.Infinite, race.Token);
        var winner = await Task.WhenAny(readTask, cancelTask).ConfigureAwait(false);
        if (!race.IsCancellationRequested)
        {
            race.Cancel();
        }

        if (winner != readTask)
        {
            _parkedResponse = readTask;
            throw new OperationCanceledException(cancellationToken);
        }

        return await readTask.ConfigureAwait(false);
    }

    private string NextId() => Interlocked.Increment(ref _nextId).ToString();

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static void EnsureOk(ResponseMessage response, string fallbackMessage)
    {
        if (response.Ok)
        {
            return;
        }

        var code = response.Error?.Code ?? GraftErrorCodes.ActionFailed;
        var message = response.Error?.Message ?? fallbackMessage;
        throw new GraftException(code, message);
    }
}
