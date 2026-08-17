using System.IO.Pipes;
using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class SendKeysDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-keys-" + Guid.NewGuid().ToString("N");

    public SendKeysDispatchTests()
    {
        ClearGraftEnvironment();
        Agent.Stop();
        AgentServices.Reset();
    }

    public void Dispose()
    {
        Agent.Stop();
        AgentServices.Reset();
        ClearGraftEnvironment();
    }

    /// <summary>
    /// sendKeys without a registered key sender returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; ElementKeySender is null
    ///
    /// Steps:
    /// - Handshake then sendKeys
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task SendKeys_WithoutSender_ReturnsActionFailed()
    {
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendSendKeysAsync(client, "SampleTextBox", "abc");
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// sendKeys dispatches to the registered key sender.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementKeySender registered
    ///
    /// Steps:
    /// - Handshake then sendKeys
    ///
    /// Expected:
    /// - ok=true and fake received automationId + text
    /// </remarks>
    [Fact]
    public async Task SendKeys_WithFakeSender_CallsSendKeys()
    {
        var fake = new FakeElementKeySender();
        AgentServices.RegisterElementKeySender(fake);
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendSendKeysAsync(client, "SampleTextBox", "typed");
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleTextBox", fake.LastAutomationId);
        Assert.Equal("typed", fake.LastText);
    }

    /// <summary>
    /// pressKeys without a registered key sender returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; ElementKeySender is null
    ///
    /// Steps:
    /// - Handshake then pressKeys
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task PressKeys_WithoutSender_ReturnsActionFailed()
    {
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendPressKeysAsync(client, "SampleTextBox", "Control+A");
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// pressKeys dispatches to the registered key sender.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementKeySender registered
    ///
    /// Steps:
    /// - Handshake then pressKeys
    ///
    /// Expected:
    /// - ok=true and fake received automationId + keys
    /// </remarks>
    [Fact]
    public async Task PressKeys_WithFakeSender_CallsPressKeys()
    {
        var fake = new FakeElementKeySender();
        AgentServices.RegisterElementKeySender(fake);
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendPressKeysAsync(client, "SampleTextBox", "Control+A");
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleTextBox", fake.LastPressAutomationId);
        Assert.Equal("Control+A", fake.LastKeys);
    }

    private void StartAgent()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, _pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, "secret");
        Agent.Start();
    }

    private static async Task<NamedPipeClientStream> ConnectAsync(string pipeName)
    {
        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await client.ConnectAsync(200).ConfigureAwait(false);
                return client;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        await client.DisposeAsync().ConfigureAwait(false);
        throw new TimeoutException($"Could not connect to pipe '{pipeName}'.", last);
    }

    private static async Task<ResponseMessage> SendHandshakeAsync(Stream stream)
    {
        using var paramsDoc = JsonDocument.Parse("""{"token":"secret"}""");
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "1",
            Method = ProtocolMethods.Handshake,
            Params = paramsDoc.RootElement.Clone(),
        };
        await JsonMessageCodec.WriteRequestAsync(stream, request);
        return await JsonMessageCodec.ReadResponseAsync(stream);
    }

    private static async Task<ResponseMessage> SendSendKeysAsync(Stream stream, string automationId, string text)
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.SendKeys,
            Params = JsonSerializer.SerializeToElement(new { automationId, text }),
        };
        await JsonMessageCodec.WriteRequestAsync(stream, request);
        return await JsonMessageCodec.ReadResponseAsync(stream);
    }

    private static async Task<ResponseMessage> SendPressKeysAsync(Stream stream, string automationId, string keys)
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "3",
            Method = ProtocolMethods.PressKeys,
            Params = JsonSerializer.SerializeToElement(new { automationId, keys }),
        };
        await JsonMessageCodec.WriteRequestAsync(stream, request);
        return await JsonMessageCodec.ReadResponseAsync(stream);
    }

    private static void ClearGraftEnvironment()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, null);
    }

    private sealed class FakeElementKeySender : IElementKeySender
    {
        public string? LastAutomationId { get; private set; }

        public string? LastText { get; private set; }

        public string? LastPressAutomationId { get; private set; }

        public string? LastKeys { get; private set; }

        public void SendKeys(ElementSelector selector, string text)
        {
            LastAutomationId = selector.AutomationId;
            LastText = text;
        }

        public void PressKeys(ElementSelector selector, string keys)
        {
            LastPressAutomationId = selector.AutomationId;
            LastKeys = keys;
        }
    }
}
