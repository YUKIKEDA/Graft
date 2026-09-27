using System.Text.Json;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Dialogs;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;
using DispatchResult = (Graft.Protocol.Messages.ResponseMessage Response, bool CloseAfterWrite, byte[]? BinaryFollowUp);

namespace Graft.Instrumentation.Pipe;

#if GRAFT_TEST

internal sealed partial class AgentPipeServer
{
    private static readonly Dictionary<string, Func<AgentPipeServer, RequestMessage, DispatchResult>> Handlers = BuildHandlers();

    /// <summary>
    /// Gets the wire method names this agent dispatches.
    /// </summary>
    internal static IReadOnlyCollection<string> DispatchedMethods => Handlers.Keys;

    private DispatchResult Dispatch(RequestMessage request, bool handshaken)
    {
        if (request.V != ProtocolVersion.Current)
        {
            var message =
                $"Protocol version mismatch: the client sent v={request.V}, but this agent (Graft.Instrumentation {AgentVersion}) speaks v={ProtocolVersion.Current}. "
                + "Update Graft.Core and Graft.Instrumentation / Graft.Instrumentation.Wpf to the same Graft release.";
            return (Error(request.Id, GraftErrorCodes.ProtocolVersionMismatch, message), true, null);
        }

        if (!handshaken && request.Method != ProtocolMethods.Handshake)
        {
            return (Error(request.Id, GraftErrorCodes.HandshakeRejected, "Handshake is required before other methods."), true, null);
        }

        if (!Handlers.TryGetValue(request.Method, out var handler))
        {
            return (Error(request.Id, GraftErrorCodes.ActionFailed, $"Method '{request.Method}' is not implemented."), false, null);
        }

        return handler(this, request);
    }

    private DispatchResult Handshake(RequestMessage request)
    {
        var token = RequestParamsReader.ReadHandshakeToken(request.Params);
        if (!IsTokenValid(token))
        {
            return (Error(request.Id, GraftErrorCodes.HandshakeRejected, "Connect token rejected."), true, null);
        }

        return (Ok(request.Id), false, null);
    }

    private static DispatchResult Screenshot(RequestMessage request)
    {
        var provider = AgentServices.ScreenshotProvider;
        if (provider is null)
        {
            return (
                Error(request.Id, GraftErrorCodes.ActionFailed, "No screenshot provider is registered. Call WpfGraft.Use() before Agent.Start()."),
                false,
                null
            );
        }

        try
        {
            var options = RequestParamsReader.ReadScreenshotOptions(request.Params);
            var capture = provider.Capture(options);
            var resultJson = JsonSerializer.SerializeToElement(capture.Meta, JsonMessageCodec.Options);
            return (Ok(request.Id, resultJson), false, capture.PngBytes);
        }
        catch (Exception ex)
        {
            return (MapScreenshot(request, ex), false, null);
        }
    }

    private static Dictionary<string, Func<AgentPipeServer, RequestMessage, DispatchResult>> BuildHandlers()
    {
        const string use = "Call WpfGraft.Use() before Agent.Start().";
        const string invokerMissing = "No element invoker is registered. " + use;
        const string valueMissing = "No element value setter is registered. " + use;
        const string togglerMissing = "No element toggler is registered. " + use;
        const string keysMissing = "No element key sender is registered. " + use;
        const string scrollerMissing = "No element scroller is registered. " + use;
        const string chooserMissing = "No element chooser is registered. " + use;
        const string menuMissing = "No menu selector is registered. " + use;
        const string treeSelectorMissing = "No tree selector is registered. " + use;
        const string cellMissing = "No element cell accessor is registered. " + use;
        const string gridMissing = "No DataGrid operator is registered. " + use;
        const string treeMissing = "No UI tree provider is registered. " + use;
        const string windowsMissing = "No window catalog is registered. " + use;

        return new Dictionary<string, Func<AgentPipeServer, RequestMessage, DispatchResult>>(StringComparer.Ordinal)
        {
            [ProtocolMethods.Handshake] = (server, request) => server.Handshake(request),
            [ProtocolMethods.GetTree] = (_, request) =>
                Call(
                    request,
                    AgentServices.TreeProvider,
                    treeMissing,
                    provider =>
                    {
                        var result = provider.GetTree(RequestParamsReader.ReadGetTreeOptions(request.Params));
                        return Ok(request.Id, JsonSerializer.SerializeToElement(result, JsonMessageCodec.Options));
                    },
                    MapActionOnly
                ),
            [ProtocolMethods.ListWindows] = (_, request) =>
                Call(
                    request,
                    AgentServices.WindowCatalog,
                    windowsMissing,
                    catalog => Ok(request.Id, JsonSerializer.SerializeToElement(catalog.ListWindows(), JsonMessageCodec.Options)),
                    MapAllFailed
                ),
            [ProtocolMethods.SwitchWindow] = (_, request) =>
                Call(
                    request,
                    AgentServices.WindowCatalog,
                    windowsMissing,
                    catalog => catalog.SwitchWindow(RequestParamsReader.ReadWindowId(request.Params)),
                    MapResolveOnly
                ),
            [ProtocolMethods.InvokeOpeningWindow] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker => invoker.BeginInvoke(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.Screenshot] = (_, request) => Screenshot(request),
            [ProtocolMethods.Invoke] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker => invoker.Invoke(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.RightClick] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker => invoker.RightClick(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.DoubleClick] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker => invoker.DoubleClick(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.Hover] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker => invoker.Hover(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.Drag] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker =>
                    {
                        var (from, toAutomationId) = RequestParamsReader.ReadDrag(request.Params);
                        invoker.Drag(from, new ElementSelector { AutomationId = toAutomationId });
                    }
                ),
            [ProtocolMethods.ClickAt] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker =>
                    {
                        var (selector, offsetX, offsetY) = RequestParamsReader.ReadClickAt(request.Params);
                        invoker.ClickAt(selector, offsetX, offsetY);
                    }
                ),
            [ProtocolMethods.Wheel] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementInvoker,
                    invokerMissing,
                    invoker =>
                    {
                        var (selector, delta) = RequestParamsReader.ReadWheel(request.Params);
                        invoker.Wheel(selector, delta);
                    }
                ),
            [ProtocolMethods.SetValue] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementValueSetter,
                    valueMissing,
                    setter =>
                    {
                        var (selector, value) = RequestParamsReader.ReadSetValue(request.Params);
                        setter.SetValue(selector, value);
                    }
                ),
            [ProtocolMethods.Toggle] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementToggler,
                    togglerMissing,
                    toggler => toggler.Toggle(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.SendKeys] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementKeySender,
                    keysMissing,
                    sender =>
                    {
                        var (selector, text) = RequestParamsReader.ReadSendKeys(request.Params);
                        sender.SendKeys(selector, text);
                    }
                ),
            [ProtocolMethods.TypeHuman] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementKeySender,
                    keysMissing,
                    sender =>
                    {
                        var (selector, text, delay) = RequestParamsReader.ReadTypeHuman(request.Params);
                        sender.TypeHuman(selector, text, delay);
                    }
                ),
            [ProtocolMethods.PressKeys] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementKeySender,
                    keysMissing,
                    sender =>
                    {
                        var (selector, keys) = RequestParamsReader.ReadPressKeys(request.Params);
                        sender.PressKeys(selector, keys);
                    }
                ),
            [ProtocolMethods.ScrollIntoView] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementScroller,
                    scrollerMissing,
                    scroller =>
                    {
                        var (selector, index) = RequestParamsReader.ReadScrollIntoView(request.Params);
                        var identity = scroller.ScrollIntoView(selector, index);
                        return Ok(request.Id, JsonSerializer.SerializeToElement(identity, JsonMessageCodec.Options));
                    }
                ),
            [ProtocolMethods.Select] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementChooser,
                    chooserMissing,
                    chooser =>
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
                    }
                ),
            [ProtocolMethods.SelectMany] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementChooser,
                    chooserMissing,
                    chooser =>
                    {
                        var (selector, indexes) = RequestParamsReader.ReadSelectMany(request.Params);
                        chooser.SelectMany(selector, indexes);
                    }
                ),
            [ProtocolMethods.SelectMenu] = (_, request) =>
                Call(
                    request,
                    AgentServices.MenuSelector,
                    menuMissing,
                    menu =>
                    {
                        var (selector, path) = RequestParamsReader.ReadElementPath(request.Params);
                        menu.SelectMenu(selector, path);
                    }
                ),
            [ProtocolMethods.SelectTree] = (_, request) =>
                Call(
                    request,
                    AgentServices.TreeSelector,
                    treeSelectorMissing,
                    tree =>
                    {
                        var (selector, path) = RequestParamsReader.ReadElementPath(request.Params);
                        tree.SelectTree(selector, path);
                    }
                ),
            [ProtocolMethods.Expand] = (_, request) => Expand(request, expand: true),
            [ProtocolMethods.Collapse] = (_, request) => Expand(request, expand: false),
            [ProtocolMethods.GetCellText] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementCellAccessor,
                    cellMissing,
                    accessor =>
                    {
                        var (selector, row, column, columnKey) = RequestParamsReader.ReadCell(request.Params);
                        var text = accessor.GetCellText(selector, row, column, columnKey);
                        return Ok(request.Id, JsonSerializer.SerializeToElement(new CellTextResult { Text = text }, JsonMessageCodec.Options));
                    }
                ),
            [ProtocolMethods.SetCellValue] = (_, request) =>
                Call(
                    request,
                    AgentServices.ElementCellAccessor,
                    cellMissing,
                    accessor =>
                    {
                        var (selector, row, column, columnKey, value) = RequestParamsReader.ReadSetCellValue(request.Params);
                        accessor.SetCellValue(selector, row, column, columnKey, value);
                    }
                ),
            [ProtocolMethods.SelectCell] = (_, request) =>
                Call(
                    request,
                    AgentServices.DataGridOperator,
                    gridMissing,
                    grid =>
                    {
                        var (selector, row, column, columnKey) = RequestParamsReader.ReadCell(request.Params);
                        grid.SelectCell(selector, row, column, columnKey);
                    }
                ),
            [ProtocolMethods.SelectRow] = (_, request) =>
                Call(
                    request,
                    AgentServices.DataGridOperator,
                    gridMissing,
                    grid =>
                    {
                        var (selector, columnKey, value) = RequestParamsReader.ReadSelectRow(request.Params);
                        grid.SelectRow(selector, columnKey, value);
                    }
                ),
            [ProtocolMethods.ClickColumnHeader] = (_, request) =>
                Call(
                    request,
                    AgentServices.DataGridOperator,
                    gridMissing,
                    grid =>
                    {
                        var (selector, columnKey) = RequestParamsReader.ReadColumnKey(request.Params);
                        grid.ClickColumnHeader(selector, columnKey);
                    }
                ),
            [ProtocolMethods.AddRow] = (_, request) =>
                Call(request, AgentServices.DataGridOperator, gridMissing, grid => grid.AddRow(RequestParamsReader.ReadSelector(request.Params))),
            [ProtocolMethods.DeleteSelectedRows] = (_, request) =>
                Call(
                    request,
                    AgentServices.DataGridOperator,
                    gridMissing,
                    grid => grid.DeleteSelectedRows(RequestParamsReader.ReadSelector(request.Params))
                ),
            [ProtocolMethods.ArmOpenFile] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        OpenFileArm.ArmPath(RequestParamsReader.ReadDialogPath(request.Params));
                        return Ok(request.Id);
                    },
                    MapResolveOnly
                ),
            [ProtocolMethods.ArmOpenFileCancel] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        OpenFileArm.ArmCancel();
                        return Ok(request.Id);
                    },
                    MapAllFailed
                ),
            [ProtocolMethods.ArmSaveFile] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        SaveFileArm.ArmPath(RequestParamsReader.ReadDialogPath(request.Params));
                        return Ok(request.Id);
                    },
                    MapResolveOnly
                ),
            [ProtocolMethods.ArmSaveFileCancel] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        SaveFileArm.ArmCancel();
                        return Ok(request.Id);
                    },
                    MapAllFailed
                ),
            [ProtocolMethods.ArmOpenFolder] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        OpenFolderArm.ArmPath(RequestParamsReader.ReadDialogPath(request.Params));
                        return Ok(request.Id);
                    },
                    MapResolveOnly
                ),
            [ProtocolMethods.ArmOpenFolderCancel] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        OpenFolderArm.ArmCancel();
                        return Ok(request.Id);
                    },
                    MapAllFailed
                ),
            [ProtocolMethods.ArmMessageBox] = (_, request) =>
                Guard(
                    request,
                    () =>
                    {
                        MessageBoxArm.ArmResult(RequestParamsReader.ReadMessageBoxResult(request.Params));
                        return Ok(request.Id);
                    },
                    MapMessageBox
                ),
        };
    }

    private static DispatchResult Expand(RequestMessage request, bool expand)
    {
        const string missing = "No element expander is registered. Call WpfGraft.Use() before Agent.Start().";
        return Call(
            request,
            AgentServices.ElementExpander,
            missing,
            expander =>
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
            }
        );
    }

    private static DispatchResult Call<TService>(
        RequestMessage request,
        TService? service,
        string missing,
        Action<TService> body,
        Func<RequestMessage, Exception, ResponseMessage>? map = null
    )
        where TService : class =>
        Call(
            request,
            service,
            missing,
            service =>
            {
                body(service);
                return Ok(request.Id);
            },
            map
        );

    private static DispatchResult Call<TService>(
        RequestMessage request,
        TService? service,
        string missing,
        Func<TService, ResponseMessage> body,
        Func<RequestMessage, Exception, ResponseMessage>? map = null
    )
        where TService : class
    {
        if (service is null)
        {
            return (Error(request.Id, GraftErrorCodes.ActionFailed, missing), false, null);
        }

        return Guard(request, () => body(service), map);
    }

    private static DispatchResult Guard(
        RequestMessage request,
        Func<ResponseMessage> body,
        Func<RequestMessage, Exception, ResponseMessage>? map = null
    )
    {
        try
        {
            return (body(), false, null);
        }
        catch (Exception ex)
        {
            return ((map ?? MapStandard)(request, ex), false, null);
        }
    }

    private static ResponseMessage MapStandard(RequestMessage request, Exception ex) =>
        ex switch
        {
            InvalidParamsException invalid => Error(request.Id, GraftErrorCodes.SelectorInvalid, invalid.Message),
            ElementResolveException resolve => Error(request.Id, resolve.Code, resolve.Message),
            ElementActionException action => Error(request.Id, action.Code, action.Message),
            _ => Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message),
        };

    // getTree reports resolve failures as action.failed. ReadGetTreeOptions already swallows a bad payload.
    private static ResponseMessage MapActionOnly(RequestMessage request, Exception ex) =>
        ex switch
        {
            ElementActionException action => Error(request.Id, action.Code, action.Message),
            _ => Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message),
        };

    // switchWindow and dialog arms report action failures as action.failed.
    private static ResponseMessage MapResolveOnly(RequestMessage request, Exception ex) =>
        ex switch
        {
            InvalidParamsException invalid => Error(request.Id, GraftErrorCodes.SelectorInvalid, invalid.Message),
            ElementResolveException resolve => Error(request.Id, resolve.Code, resolve.Message),
            _ => Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message),
        };

    // listWindows and arm-cancel report every failure as action.failed.
    private static ResponseMessage MapAllFailed(RequestMessage request, Exception ex) => Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message);

    private static ResponseMessage MapScreenshot(RequestMessage request, Exception ex)
    {
        if (ex is InvalidOperationException && ex.Message.Contains("Main window", StringComparison.OrdinalIgnoreCase))
        {
            return Error(request.Id, GraftErrorCodes.WindowNotFound, ex.Message);
        }

        return MapStandard(request, ex);
    }

    private static ResponseMessage MapMessageBox(RequestMessage request, Exception ex) =>
        ex switch
        {
            ArgumentException => Error(request.Id, GraftErrorCodes.SelectorInvalid, ex.Message),
            InvalidParamsException invalid => Error(request.Id, GraftErrorCodes.SelectorInvalid, invalid.Message),
            ElementResolveException resolve => Error(request.Id, resolve.Code, resolve.Message),
            _ => Error(request.Id, GraftErrorCodes.ActionFailed, ex.Message),
        };
}

#endif
