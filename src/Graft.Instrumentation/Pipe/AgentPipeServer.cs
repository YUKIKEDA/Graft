using System.IO.Pipes;
using System.Text.Json;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Pipe;

#if GRAFT_TEST

/// <summary>
/// Named-pipe listener for a single client with reconnect after disconnect.
/// </summary>
/// <remarks>
/// Uses <see cref="PipeOptions.CurrentUserOnly"/> (same-user ACL). At most one
/// <see cref="NamedPipeServerStream"/> instance is created at a time so a second
/// client cannot complete a connection while the first is active.
/// </remarks>
internal sealed class AgentPipeServer : IDisposable
{
    private static readonly string AgentVersion = DescribeVersion(typeof(AgentPipeServer).Assembly);

    private readonly string _pipeName;
    private readonly string _connectToken;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private bool _disposed;

    /// <summary>
    /// Initializes and starts the accept loop.
    /// </summary>
    /// <param name="pipeName">Pipe name (without <c>\\.\pipe\</c> prefix).</param>
    /// <param name="connectToken">Expected handshake token.</param>
    public AgentPipeServer(string pipeName, string connectToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectToken);
        _pipeName = pipeName;
        _connectToken = connectToken;
        _loop = RunAsync(_cts.Token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        try
        {
            _ = _loop.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Shutdown races with accept/read cancellation.
        }

        _cts.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = CreateServer();
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await HandleConnectionAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // Client disconnected or pipe broken; accept again.
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // A single misbehaving connection must never take down the accept loop.
                System.Diagnostics.Trace.TraceWarning($"Graft agent pipe: connection dropped after unexpected error: {ex}");
            }
            finally
            {
                if (server is not null)
                {
                    await server.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private NamedPipeServerStream CreateServer() =>
        new(
            _pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
        );

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        var handshaken = false;

        while (!cancellationToken.IsCancellationRequested && server.IsConnected)
        {
            RequestMessage request;
            try
            {
                request = await JsonMessageCodec.ReadRequestAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                // Malformed JSON, null envelope, or an invalid / oversized length prefix. The stream
                // position is no longer trustworthy, so drop this connection and accept a new one.
                System.Diagnostics.Trace.TraceWarning($"Graft agent pipe: dropping connection after malformed frame: {ex.Message}");
                break;
            }

            (ResponseMessage Response, bool CloseAfterWrite, byte[]? BinaryFollowUp) dispatched;
            try
            {
                dispatched = Dispatch(request, handshaken);
            }
            catch (Exception ex)
            {
                dispatched = (
                    Error(request.Id ?? string.Empty, GraftErrorCodes.ActionFailed, ex.Message),
                    CloseAfterWrite: false,
                    BinaryFollowUp: null
                );
            }

            var (response, closeAfterWrite, binaryFollowUp) = dispatched;
            if (response.Ok && request.Method == ProtocolMethods.Handshake)
            {
                handshaken = true;
            }

            try
            {
                await JsonMessageCodec.WriteResponseAsync(server, response, cancellationToken).ConfigureAwait(false);

                if (binaryFollowUp is { Length: > 0 })
                {
                    await FrameIO.WriteAsync(server, binaryFollowUp, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                break;
            }

            if (closeAfterWrite)
            {
                break;
            }
        }
    }

    private (ResponseMessage Response, bool CloseAfterWrite, byte[]? BinaryFollowUp) Dispatch(RequestMessage request, bool handshaken)
    {
        if (request.V != ProtocolVersion.Current)
        {
            return (
                Error(
                    request.Id,
                    GraftErrorCodes.ProtocolVersionMismatch,
                    $"Protocol version mismatch: the client sent v={request.V}, but this agent (Graft.Instrumentation {AgentVersion}) "
                        + $"speaks v={ProtocolVersion.Current}. Update Graft.Core and Graft.Instrumentation / Graft.Instrumentation.Wpf "
                        + "to the same Graft release."
                ),
                CloseAfterWrite: true,
                BinaryFollowUp: null
            );
        }

        if (!handshaken)
        {
            if (request.Method != ProtocolMethods.Handshake)
            {
                return (
                    Error(request.Id, GraftErrorCodes.HandshakeRejected, "Handshake is required before other methods."),
                    CloseAfterWrite: true,
                    BinaryFollowUp: null
                );
            }

            var token = RequestParamsReader.ReadHandshakeToken(request.Params);
            if (!IsTokenValid(token))
            {
                return (Error(request.Id, GraftErrorCodes.HandshakeRejected, "Connect token rejected."), CloseAfterWrite: true, BinaryFollowUp: null);
            }

            return (Ok(request.Id), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Handshake)
        {
            var token = RequestParamsReader.ReadHandshakeToken(request.Params);
            if (!IsTokenValid(token))
            {
                return (Error(request.Id, GraftErrorCodes.HandshakeRejected, "Connect token rejected."), CloseAfterWrite: true, BinaryFollowUp: null);
            }

            return (Ok(request.Id), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.GetTree)
        {
            return (HandleGetTree(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Screenshot)
        {
            return HandleScreenshot(request);
        }

        if (request.Method == ProtocolMethods.Invoke)
        {
            return (HandleInvoke(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.RightClick)
        {
            return (HandleRightClick(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.DoubleClick)
        {
            return (HandleDoubleClick(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Hover)
        {
            return (HandleHover(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Drag)
        {
            return (HandleDrag(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ClickAt)
        {
            return (HandleClickAt(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Wheel)
        {
            return (HandleWheel(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SetValue)
        {
            return (HandleSetValue(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Toggle)
        {
            return (HandleToggle(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SendKeys)
        {
            return (HandleSendKeys(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.TypeHuman)
        {
            return (HandleTypeHuman(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.PressKeys)
        {
            return (HandlePressKeys(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ScrollIntoView)
        {
            return (HandleScrollIntoView(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Select)
        {
            return (HandleSelect(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SelectMany)
        {
            return (HandleSelectMany(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SelectMenu)
        {
            return (HandleSelectMenu(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SelectTree)
        {
            return (HandleSelectTree(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.GetCellText)
        {
            return (HandleGetCellText(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SetCellValue)
        {
            return (HandleSetCellValue(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SelectCell)
        {
            return (HandleSelectCell(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SelectRow)
        {
            return (HandleSelectRow(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ClickColumnHeader)
        {
            return (HandleClickColumnHeader(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.AddRow)
        {
            return (HandleAddRow(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.DeleteSelectedRows)
        {
            return (HandleDeleteSelectedRows(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmOpenFile)
        {
            return (HandleArmOpenFile(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmOpenFileCancel)
        {
            return (HandleArmOpenFileCancel(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmSaveFile)
        {
            return (HandleArmSaveFile(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmSaveFileCancel)
        {
            return (HandleArmSaveFileCancel(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmOpenFolder)
        {
            return (HandleArmOpenFolder(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmOpenFolderCancel)
        {
            return (HandleArmOpenFolderCancel(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ArmMessageBox)
        {
            return (HandleArmMessageBox(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Expand)
        {
            return (HandleExpand(request, expand: true), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Collapse)
        {
            return (HandleExpand(request, expand: false), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.ListWindows)
        {
            return (HandleListWindows(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.SwitchWindow)
        {
            return (HandleSwitchWindow(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.InvokeOpeningWindow)
        {
            return (HandleInvokeOpeningWindow(request), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        return (
            Error(request.Id, GraftErrorCodes.ActionFailed, $"Method '{request.Method}' is not implemented."),
            CloseAfterWrite: false,
            BinaryFollowUp: null
        );
    }

    private static ResponseMessage HandleGetTree(RequestMessage request)
    {
        var provider = AgentServices.TreeProvider;
        if (provider is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No UI tree provider is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var options = RequestParamsReader.ReadGetTreeOptions(request.Params);
            var result = provider.GetTree(options);
            var resultJson = JsonSerializer.SerializeToElement(result, JsonMessageCodec.Options);
            return Ok(request.Id, resultJson);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static (ResponseMessage Response, bool CloseAfterWrite, byte[]? BinaryFollowUp) HandleScreenshot(RequestMessage request)
    {
        var provider = AgentServices.ScreenshotProvider;
        if (provider is null)
        {
            return (
                Error(request.Id, GraftErrorCodes.ActionFailed, "No screenshot provider is registered. Call WpfGraft.Use() before Agent.Start()."),
                CloseAfterWrite: false,
                BinaryFollowUp: null
            );
        }

        try
        {
            var options = RequestParamsReader.ReadScreenshotOptions(request.Params);
            var capture = provider.Capture(options);
            var resultJson = JsonSerializer.SerializeToElement(capture.Meta, JsonMessageCodec.Options);
            return (Ok(request.Id, resultJson), CloseAfterWrite: false, capture.PngBytes);
        }
        catch (ElementResolveException ex)
        {
            return (Error(request.Id, ex.Code, ex.Message), CloseAfterWrite: false, BinaryFollowUp: null);
        }
        catch (ElementActionException ex)
        {
            return (Error(request.Id, ex.Code, ex.Message), CloseAfterWrite: false, BinaryFollowUp: null);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Main window", StringComparison.OrdinalIgnoreCase))
        {
            return (Error(request.Id, GraftErrorCodes.WindowNotFound, ex.Message), CloseAfterWrite: false, BinaryFollowUp: null);
        }
        catch (Exception ex)
        {
            return (Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message), CloseAfterWrite: false, BinaryFollowUp: null);
        }
    }

    private static ResponseMessage HandleInvoke(RequestMessage request)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            invoker.Invoke(selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleRightClick(RequestMessage request)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            invoker.RightClick(selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleDoubleClick(RequestMessage request) =>
        HandleInvokerAction(request, static (invoker, selector) => invoker.DoubleClick(selector));

    private static ResponseMessage HandleHover(RequestMessage request) =>
        HandleInvokerAction(request, static (invoker, selector) => invoker.Hover(selector));

    private static ResponseMessage HandleDrag(RequestMessage request)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (from, toAutomationId) = RequestParamsReader.ReadDrag(request.Params);
            var to = new ElementSelector { AutomationId = toAutomationId };
            invoker.Drag(from, to);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleClickAt(RequestMessage request)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, offsetX, offsetY) = RequestParamsReader.ReadClickAt(request.Params);
            invoker.ClickAt(selector, offsetX, offsetY);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleWheel(RequestMessage request)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, delta) = RequestParamsReader.ReadWheel(request.Params);
            invoker.Wheel(selector, delta);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleInvokerAction(RequestMessage request, Action<IElementInvoker, ElementSelector> action)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            action(invoker, selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSetValue(RequestMessage request)
    {
        var setter = AgentServices.ElementValueSetter;
        if (setter is null)
        {
            return Error(
                request.Id,
                GraftErrorCodes.ActionFailed,
                "No element value setter is registered. Call WpfGraft.Use() before Agent.Start()."
            );
        }

        try
        {
            var (selector, value) = RequestParamsReader.ReadSetValue(request.Params);
            setter.SetValue(selector, value);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleToggle(RequestMessage request)
    {
        var toggler = AgentServices.ElementToggler;
        if (toggler is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element toggler is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            toggler.Toggle(selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSendKeys(RequestMessage request)
    {
        var keySender = AgentServices.ElementKeySender;
        if (keySender is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element key sender is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, text) = RequestParamsReader.ReadSendKeys(request.Params);
            keySender.SendKeys(selector, text);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleTypeHuman(RequestMessage request)
    {
        var keySender = AgentServices.ElementKeySender;
        if (keySender is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element key sender is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, text, delay) = RequestParamsReader.ReadTypeHuman(request.Params);
            keySender.TypeHuman(selector, text, delay);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandlePressKeys(RequestMessage request)
    {
        var keySender = AgentServices.ElementKeySender;
        if (keySender is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element key sender is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, keys) = RequestParamsReader.ReadPressKeys(request.Params);
            keySender.PressKeys(selector, keys);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleScrollIntoView(RequestMessage request)
    {
        var scroller = AgentServices.ElementScroller;
        if (scroller is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element scroller is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, index) = RequestParamsReader.ReadScrollIntoView(request.Params);
            var identity = scroller.ScrollIntoView(selector, index);
            var resultJson = JsonSerializer.SerializeToElement(identity, JsonMessageCodec.Options);
            return Ok(request.Id, resultJson);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSelect(RequestMessage request)
    {
        var chooser = AgentServices.ElementChooser;
        if (chooser is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element chooser is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, index, key) = RequestParamsReader.ReadSelect(request.Params);
            if (index is not null)
            {
                chooser.Select(selector, index.Value);
            }
            else
            {
                chooser.Select(selector, key!);
            }

            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSelectMany(RequestMessage request)
    {
        var chooser = AgentServices.ElementChooser;
        if (chooser is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element chooser is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, indexes) = RequestParamsReader.ReadSelectMany(request.Params);
            chooser.SelectMany(selector, indexes);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSelectMenu(RequestMessage request)
    {
        var menuSelector = AgentServices.MenuSelector;
        if (menuSelector is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No menu selector is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, path) = RequestParamsReader.ReadElementPath(request.Params);
            menuSelector.SelectMenu(selector, path);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSelectTree(RequestMessage request)
    {
        var treeSelector = AgentServices.TreeSelector;
        if (treeSelector is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No tree selector is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, path) = RequestParamsReader.ReadElementPath(request.Params);
            treeSelector.SelectTree(selector, path);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleGetCellText(RequestMessage request)
    {
        var accessor = AgentServices.ElementCellAccessor;
        if (accessor is null)
        {
            return Error(
                request.Id,
                GraftErrorCodes.ActionFailed,
                "No element cell accessor is registered. Call WpfGraft.Use() before Agent.Start()."
            );
        }

        try
        {
            var (selector, row, column, columnKey) = RequestParamsReader.ReadCell(request.Params);
            var text = accessor.GetCellText(selector, row, column, columnKey);
            var resultJson = JsonSerializer.SerializeToElement(new CellTextResult { Text = text }, JsonMessageCodec.Options);
            return Ok(request.Id, resultJson);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSetCellValue(RequestMessage request)
    {
        var accessor = AgentServices.ElementCellAccessor;
        if (accessor is null)
        {
            return Error(
                request.Id,
                GraftErrorCodes.ActionFailed,
                "No element cell accessor is registered. Call WpfGraft.Use() before Agent.Start()."
            );
        }

        try
        {
            var (selector, row, column, columnKey, value) = RequestParamsReader.ReadSetCellValue(request.Params);
            accessor.SetCellValue(selector, row, column, columnKey, value);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSelectCell(RequestMessage request)
    {
        var op = AgentServices.DataGridOperator;
        if (op is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No DataGrid operator is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, row, column, columnKey) = RequestParamsReader.ReadCell(request.Params);
            op.SelectCell(selector, row, column, columnKey);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSelectRow(RequestMessage request)
    {
        var op = AgentServices.DataGridOperator;
        if (op is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No DataGrid operator is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, columnKey, value) = RequestParamsReader.ReadSelectRow(request.Params);
            op.SelectRow(selector, columnKey, value);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleClickColumnHeader(RequestMessage request)
    {
        var op = AgentServices.DataGridOperator;
        if (op is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No DataGrid operator is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, columnKey) = RequestParamsReader.ReadColumnKey(request.Params);
            op.ClickColumnHeader(selector, columnKey);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleAddRow(RequestMessage request)
    {
        var op = AgentServices.DataGridOperator;
        if (op is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No DataGrid operator is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            op.AddRow(selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleDeleteSelectedRows(RequestMessage request)
    {
        var op = AgentServices.DataGridOperator;
        if (op is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No DataGrid operator is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            op.DeleteSelectedRows(selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmOpenFile(RequestMessage request)
    {
        try
        {
            var path = RequestParamsReader.ReadDialogPath(request.Params);
            OpenFileArm.ArmPath(path);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmOpenFileCancel(RequestMessage request)
    {
        try
        {
            OpenFileArm.ArmCancel();
            return Ok(request.Id);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmSaveFile(RequestMessage request)
    {
        try
        {
            var path = RequestParamsReader.ReadDialogPath(request.Params);
            SaveFileArm.ArmPath(path);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmSaveFileCancel(RequestMessage request)
    {
        try
        {
            SaveFileArm.ArmCancel();
            return Ok(request.Id);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmOpenFolder(RequestMessage request)
    {
        try
        {
            var path = RequestParamsReader.ReadDialogPath(request.Params);
            OpenFolderArm.ArmPath(path);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmOpenFolderCancel(RequestMessage request)
    {
        try
        {
            OpenFolderArm.ArmCancel();
            return Ok(request.Id);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleArmMessageBox(RequestMessage request)
    {
        try
        {
            var result = RequestParamsReader.ReadMessageBoxResult(request.Params);
            MessageBoxArm.ArmResult(result);
            return Ok(request.Id);
        }
        catch (ArgumentException ex)
        {
            return Error(request.Id, GraftErrorCodes.SelectorInvalid, ex.Message);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleExpand(RequestMessage request, bool expand)
    {
        var expander = AgentServices.ElementExpander;
        if (expander is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element expander is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            if (expand)
            {
                expander.Expand(selector);
            }
            else
            {
                expander.Collapse(selector);
            }

            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleListWindows(RequestMessage request)
    {
        var catalog = AgentServices.WindowCatalog;
        if (catalog is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No window catalog is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var result = catalog.ListWindows();
            var resultJson = JsonSerializer.SerializeToElement(result, JsonMessageCodec.Options);
            return Ok(request.Id, resultJson);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleSwitchWindow(RequestMessage request)
    {
        var catalog = AgentServices.WindowCatalog;
        if (catalog is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No window catalog is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var windowId = RequestParamsReader.ReadWindowId(request.Params);
            catalog.SwitchWindow(windowId);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static ResponseMessage HandleInvokeOpeningWindow(RequestMessage request)
    {
        var invoker = AgentServices.ElementInvoker;
        if (invoker is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element invoker is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var selector = RequestParamsReader.ReadSelector(request.Params);
            invoker.BeginInvoke(selector);
            return Ok(request.Id);
        }
        catch (ElementResolveException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (ElementActionException ex)
        {
            return Error(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);
        }
    }

    private static string DescribeVersion(System.Reflection.Assembly assembly) =>
        assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";

    private bool IsTokenValid(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(_connectToken)
        );
    }

    private static ResponseMessage Ok(string id, JsonElement? result = null) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Ok = true,
            Result = result,
        };

    private static ResponseMessage Error(string id, string code, string message) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Ok = false,
            Error = new ErrorObject { Code = code, Message = message },
        };
}

#endif
