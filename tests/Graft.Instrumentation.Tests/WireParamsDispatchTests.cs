using System.Reflection;
using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class WireParamsDispatchTests : IDisposable
{
    private const string Missing = "missing";
    private const string WrongType = "wrong-type";

    private static readonly JsonDocument WrongTypeDocument = JsonDocument.Parse(
        """
        {
          "automationId": 1,
          "token": 1,
          "path": 1,
          "value": 1,
          "text": 1,
          "keys": 1,
          "toAutomationId": 1,
          "columnKey": 1,
          "key": 1,
          "result": 1,
          "offsetX": "x",
          "offsetY": "y",
          "delta": "d",
          "delayMs": "d",
          "windowId": "w",
          "row": "r",
          "column": "c",
          "index": "i",
          "indexes": "n",
          "depth": "d",
          "maxNodes": "m"
        }
        """
    );

    /// <summary>
    /// Expected error code for omitted params and for a wrong JSON type. Null means the call succeeds.
    /// </summary>
    /// <remarks>
    /// getTree, screenshot, and handshake swallow a bad payload. listWindows and the arm-cancel methods do not read params.
    /// Methods whose only target is an optional selector succeed when params are omitted and reject a wrong JSON type.
    /// </remarks>
    private static readonly Dictionary<string, (string? Missing, string? WrongType)> Expected = new(StringComparer.Ordinal)
    {
        [ProtocolMethods.Handshake] = (GraftErrorCodes.HandshakeRejected, GraftErrorCodes.HandshakeRejected),
        [ProtocolMethods.GetTree] = (null, null),
        [ProtocolMethods.ListWindows] = (null, null),
        [ProtocolMethods.SwitchWindow] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.InvokeOpeningWindow] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Screenshot] = (null, null),
        [ProtocolMethods.Invoke] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.RightClick] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.DoubleClick] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Hover] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Drag] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ClickAt] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Wheel] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SetValue] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Toggle] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SendKeys] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.TypeHuman] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.PressKeys] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ScrollIntoView] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Select] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SelectMany] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SelectMenu] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SelectTree] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Expand] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.Collapse] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.GetCellText] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SetCellValue] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SelectCell] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.SelectRow] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ClickColumnHeader] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.AddRow] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.DeleteSelectedRows] = (null, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ArmOpenFile] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ArmOpenFileCancel] = (null, null),
        [ProtocolMethods.ArmSaveFile] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ArmSaveFileCancel] = (null, null),
        [ProtocolMethods.ArmOpenFolder] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
        [ProtocolMethods.ArmOpenFolderCancel] = (null, null),
        [ProtocolMethods.ArmMessageBox] = (GraftErrorCodes.SelectorInvalid, GraftErrorCodes.SelectorInvalid),
    };

    private readonly string _pipeName = "graft-params-" + Guid.NewGuid().ToString("N");

    public WireParamsDispatchTests()
    {
        PipeTestClient.ClearEnvironment();
        Agent.Stop();
        AgentServices.Reset();
        RecordingServices.RegisterAll();
        PipeTestClient.Start(_pipeName);
    }

    public void Dispose()
    {
        Agent.Stop();
        AgentServices.Reset();
        PipeTestClient.ClearEnvironment();
    }

    /// <summary>
    /// Gets every wire method twice: once with params omitted and once with the wrong JSON types.
    /// </summary>
    public static IEnumerable<object[]> Cases()
    {
        foreach (var method in ProtocolMethodNames())
        {
            yield return new object[] { method, Missing };
            yield return new object[] { method, WrongType };
        }
    }

    /// <summary>
    /// Every wire method rejects omitted params or a wrong JSON type with the code in <see cref="Expected"/>, or succeeds when that payload is legal.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A recording fake is registered for every agent service
    /// - Agent started with token secret
    ///
    /// Steps:
    /// - For handshake, send that method as the first request
    /// - For every other method, handshake, then send the method with params omitted or with wrong JSON types
    ///
    /// Expected:
    /// - The response code matches <see cref="Expected"/> for that method and payload
    /// - A method missing from <see cref="Expected"/> fails the row
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task BadParams_ReturnsTheExpectedError(string method, string payload)
    {
        Assert.True(Expected.TryGetValue(method, out var expected), $"Add an expected params result for '{method}'.");
        var code = payload == Missing ? expected.Missing : expected.WrongType;
        JsonElement? parameters = payload == WrongType ? WrongTypeDocument.RootElement.Clone() : null;

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        ResponseMessage response;
        if (method == ProtocolMethods.Handshake)
        {
            response = await PipeTestClient.SendAsync(client, Request(method, parameters, "1"));
        }
        else
        {
            Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);
            response = await PipeTestClient.SendAsync(client, Request(method, parameters, "2"));
        }

        if (code is null)
        {
            Assert.True(response.Ok, response.Error?.Message);
            return;
        }

        Assert.False(response.Ok);
        Assert.Equal(code, response.Error?.Code);
    }

    private static RequestMessage Request(string method, JsonElement? parameters, string id) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Method = method,
            Params = parameters,
        };

    private static IEnumerable<string> ProtocolMethodNames() =>
        typeof(ProtocolMethods)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);
}
