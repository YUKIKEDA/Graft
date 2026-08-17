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
        _pipeName = pipeName;
        _connectToken = connectToken ?? string.Empty;
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

            var (response, closeAfterWrite, binaryFollowUp) = Dispatch(request, handshaken);
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
                Error(request.Id, GraftErrorCodes.ProtocolVersionMismatch, $"Protocol version mismatch. Agent expects v={ProtocolVersion.Current}."),
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

            var token = ReadToken(request.Params);
            if (!string.Equals(token, _connectToken, StringComparison.Ordinal))
            {
                return (Error(request.Id, GraftErrorCodes.HandshakeRejected, "Connect token rejected."), CloseAfterWrite: true, BinaryFollowUp: null);
            }

            return (Ok(request.Id), CloseAfterWrite: false, BinaryFollowUp: null);
        }

        if (request.Method == ProtocolMethods.Handshake)
        {
            var token = ReadToken(request.Params);
            if (!string.Equals(token, _connectToken, StringComparison.Ordinal))
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
            var options = ReadGetTreeOptions(request.Params);
            var result = provider.GetTree(options);
            var resultJson = JsonSerializer.SerializeToElement(result, JsonMessageCodec.Options);
            return Ok(request.Id, resultJson);
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
            var options = ReadScreenshotOptions(request.Params);
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
            var selector = ReadElementSelector(request.Params);
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
            var selector = ReadElementSelector(request.Params);
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
            var from = ReadElementSelector(request.Params);
            var toAutomationId = ReadRequiredString(request.Params, "toAutomationId");
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
            var selector = ReadElementSelector(request.Params);
            var offsetX = ReadRequiredDouble(request.Params, "offsetX");
            var offsetY = ReadRequiredDouble(request.Params, "offsetY");
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
            var selector = ReadElementSelector(request.Params);
            var delta = ReadRequiredInt32(request.Params, "delta");
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
            var selector = ReadElementSelector(request.Params);
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
            var (selector, value) = ReadSetValueParams(request.Params);
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
            var selector = ReadElementSelector(request.Params);
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
            var (selector, text) = ReadSendKeysParams(request.Params);
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

    private static ResponseMessage HandlePressKeys(RequestMessage request)
    {
        var keySender = AgentServices.ElementKeySender;
        if (keySender is null)
        {
            return Error(request.Id, GraftErrorCodes.ActionFailed, "No element key sender is registered. Call WpfGraft.Use() before Agent.Start().");
        }

        try
        {
            var (selector, keys) = ReadPressKeysParams(request.Params);
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
            var (selector, index) = ReadScrollIntoViewParams(request.Params);
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
            var (selector, index, key) = ReadSelectParams(request.Params);
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
            var (selector, indexes) = ReadSelectManyParams(request.Params);
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
            var selector = ReadElementSelector(request.Params);
            var path = ReadRequiredString(request.Params, "path");
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
            var selector = ReadElementSelector(request.Params);
            var path = ReadRequiredString(request.Params, "path");
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
            var (selector, row, column, columnKey) = ReadCellColumnParams(request.Params);
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
            var (selector, row, column, columnKey, value) = ReadSetCellValueParams(request.Params);
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
            var (selector, row, column, columnKey) = ReadCellColumnParams(request.Params);
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
            var selector = ReadElementSelector(request.Params);
            var columnKey = ReadRequiredString(request.Params, "columnKey");
            var value = ReadRequiredString(request.Params, "value");
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
            var selector = ReadElementSelector(request.Params);
            var columnKey = ReadRequiredString(request.Params, "columnKey");
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
            var selector = ReadElementSelector(request.Params);
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
            var selector = ReadElementSelector(request.Params);
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
            var path = ReadRequiredString(request.Params, "path");
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
            var path = ReadRequiredString(request.Params, "path");
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
            var path = ReadRequiredString(request.Params, "path");
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
            var result = ReadRequiredString(request.Params, "result");
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
            var selector = ReadElementSelector(request.Params);
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
            var windowId = ReadWindowId(request.Params);
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
            var selector = ReadElementSelector(request.Params);
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

    private static int ReadWindowId(JsonElement? paramsElement)
    {
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.windowId is required.");
        }

        if (!element.TryGetProperty("windowId", out var windowIdProperty) || !windowIdProperty.TryGetInt32(out var windowId))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.windowId must be an integer.");
        }

        return windowId;
    }

    private static ElementSelector ReadElementSelector(JsonElement? paramsElement)
    {
        string? automationId = null;
        int? runtimeId = null;

        if (paramsElement is { } element && element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("automationId", out var automationIdProperty) && automationIdProperty.ValueKind == JsonValueKind.String)
            {
                automationId = automationIdProperty.GetString();
            }

            if (element.TryGetProperty("runtimeId", out var runtimeIdProperty) && runtimeIdProperty.TryGetInt32(out var id))
            {
                runtimeId = id;
            }
        }

        return new ElementSelector { AutomationId = automationId, RuntimeId = runtimeId };
    }

    private static ScreenshotOptions ReadScreenshotOptions(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (string.IsNullOrWhiteSpace(selector.AutomationId) && selector.RuntimeId is null)
        {
            return ScreenshotOptions.Default;
        }

        return new ScreenshotOptions { Selector = selector };
    }

    private static (ElementSelector Selector, string Value) ReadSetValueParams(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.value is required.");
        }

        if (!element.TryGetProperty("value", out var valueProperty))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.value is required.");
        }

        var value = valueProperty.ValueKind switch
        {
            JsonValueKind.String => valueProperty.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.value must be a string."),
        };

        return (selector, value);
    }

    private static (ElementSelector Selector, int? Index) ReadScrollIntoViewParams(JsonElement? paramsElement) =>
        ReadIndexParams(paramsElement, requireIndex: false);

    private static (ElementSelector Selector, int Row, int? Column, string? ColumnKey) ReadCellColumnParams(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(
                GraftErrorCodes.SelectorInvalid,
                "params.row and exactly one of params.column or params.columnKey are required."
            );
        }

        if (!element.TryGetProperty("row", out var rowProperty) || !rowProperty.TryGetInt32(out var row))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.row must be an integer.");
        }

        int? column = null;
        if (element.TryGetProperty("column", out var columnProperty))
        {
            if (!columnProperty.TryGetInt32(out var columnValue))
            {
                throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.column must be an integer.");
            }

            column = columnValue;
        }

        string? columnKey = null;
        if (element.TryGetProperty("columnKey", out var columnKeyProperty))
        {
            columnKey = columnKeyProperty.ValueKind switch
            {
                JsonValueKind.String => columnKeyProperty.GetString(),
                JsonValueKind.Null => null,
                _ => throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.columnKey must be a string."),
            };
        }

        var hasColumn = column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(columnKey);
        if (hasColumn == hasKey)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "Exactly one of params.column or params.columnKey is required.");
        }

        return (selector, row, column, hasKey ? columnKey : null);
    }

    private static (ElementSelector Selector, int Row, int? Column, string? ColumnKey, string Value) ReadSetCellValueParams(
        JsonElement? paramsElement
    )
    {
        var (selector, row, column, columnKey) = ReadCellColumnParams(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.value is required.");
        }

        if (!element.TryGetProperty("value", out var valueProperty))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.value is required.");
        }

        var value = valueProperty.ValueKind switch
        {
            JsonValueKind.String => valueProperty.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.value must be a string."),
        };

        return (selector, row, column, columnKey, value);
    }

    private static (ElementSelector Selector, int? Index) ReadIndexParams(JsonElement? paramsElement, bool requireIndex)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            if (requireIndex)
            {
                throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.index is required.");
            }

            return (selector, null);
        }

        if (!element.TryGetProperty("index", out var indexProperty))
        {
            if (requireIndex)
            {
                throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.index is required.");
            }

            return (selector, null);
        }

        if (!indexProperty.TryGetInt32(out var index))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.index must be an integer.");
        }

        return (selector, index);
    }

    private static (ElementSelector Selector, int? Index, string? Key) ReadSelectParams(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params must have exactly one of index or key.");
        }

        var hasIndex = element.TryGetProperty("index", out var indexProperty);
        var hasKey = element.TryGetProperty("key", out var keyProperty);
        if (hasIndex == hasKey)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params must have exactly one of index or key.");
        }

        if (hasIndex)
        {
            if (!indexProperty.TryGetInt32(out var index))
            {
                throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.index must be an integer.");
            }

            return (selector, index, null);
        }

        if (keyProperty.ValueKind != JsonValueKind.String)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.key must be a string.");
        }

        var key = keyProperty.GetString();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.key must be a non-empty string.");
        }

        return (selector, null, key);
    }

    private static (ElementSelector Selector, IReadOnlyList<int> Indexes) ReadSelectManyParams(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.indexes is required.");
        }

        if (!element.TryGetProperty("indexes", out var indexesProperty))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.indexes is required.");
        }

        if (indexesProperty.ValueKind != JsonValueKind.Array)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.indexes must be an array of integers.");
        }

        var indexes = new List<int>(indexesProperty.GetArrayLength());
        foreach (var entry in indexesProperty.EnumerateArray())
        {
            if (!entry.TryGetInt32(out var index))
            {
                throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.indexes must be an array of integers.");
            }

            indexes.Add(index);
        }

        return (selector, indexes);
    }

    private static (ElementSelector Selector, string Text) ReadSendKeysParams(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.text is required.");
        }

        if (!element.TryGetProperty("text", out var textProperty))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.text is required.");
        }

        var text = textProperty.ValueKind switch
        {
            JsonValueKind.String => textProperty.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.text must be a string."),
        };

        return (selector, text);
    }

    private static (ElementSelector Selector, string Keys) ReadPressKeysParams(JsonElement? paramsElement)
    {
        var selector = ReadElementSelector(paramsElement);
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.keys is required.");
        }

        if (!element.TryGetProperty("keys", out var keysProperty))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.keys is required.");
        }

        if (keysProperty.ValueKind != JsonValueKind.String)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.keys must be a string.");
        }

        var keys = keysProperty.GetString();
        if (string.IsNullOrWhiteSpace(keys))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, "params.keys must be a non-empty chord string.");
        }

        return (selector, keys);
    }

    private static GetTreeOptions ReadGetTreeOptions(JsonElement? paramsElement)
    {
        var maxDepth = GetTreeOptions.DefaultMaxDepth;
        var maxNodes = GetTreeOptions.DefaultMaxNodes;

        if (paramsElement is { } element && element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("depth", out var depthProperty) && depthProperty.TryGetInt32(out var depth) && depth >= 0)
            {
                maxDepth = depth;
            }

            if (element.TryGetProperty("maxNodes", out var maxNodesProperty) && maxNodesProperty.TryGetInt32(out var nodes) && nodes > 0)
            {
                maxNodes = nodes;
            }
        }

        return new GetTreeOptions { MaxDepth = maxDepth, MaxNodes = maxNodes };
    }

    private static string ReadToken(JsonElement? paramsElement)
    {
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (!element.TryGetProperty("token", out var tokenProperty))
        {
            return string.Empty;
        }

        return tokenProperty.ValueKind switch
        {
            JsonValueKind.String => tokenProperty.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => string.Empty,
        };
    }

    private static string ReadRequiredString(JsonElement? paramsElement, string propertyName)
    {
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} is required.");
        }

        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} is required.");
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} must be a string.");
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} must be a non-empty string.");
        }

        return value;
    }

    private static double ReadRequiredDouble(JsonElement? paramsElement, string propertyName)
    {
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} is required.");
        }

        if (
            !element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetDouble(out var value)
        )
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} must be a number.");
        }

        return value;
    }

    private static int ReadRequiredInt32(JsonElement? paramsElement, string propertyName)
    {
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} is required.");
        }

        if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetInt32(out var value))
        {
            throw new ElementResolveException(GraftErrorCodes.SelectorInvalid, $"params.{propertyName} must be an integer.");
        }

        return value;
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
