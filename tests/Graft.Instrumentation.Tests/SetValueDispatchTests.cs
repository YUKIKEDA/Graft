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

public sealed class SetValueDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-sv-" + Guid.NewGuid().ToString("N");

    public SetValueDispatchTests()
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
    /// setValue without a registered setter returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; ElementValueSetter is null
    ///
    /// Steps:
    /// - Handshake then setValue
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task SetValue_WithoutSetter_ReturnsActionFailed()
    {
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendSetValueAsync(client, "SampleTextBox", "x");
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// setValue dispatches automationId and value to the registered setter.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementValueSetter registered
    ///
    /// Steps:
    /// - Handshake then setValue
    ///
    /// Expected:
    /// - ok=true; fake received SampleTextBox and the value
    /// </remarks>
    [Fact]
    public async Task SetValue_WithFakeSetter_CallsSetValue()
    {
        var fake = new FakeElementValueSetter();
        AgentServices.RegisterElementValueSetter(fake);
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendSetValueAsync(client, "SampleTextBox", "hello");
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleTextBox", fake.LastAutomationId);
        Assert.Equal("hello", fake.LastValue);
    }

    /// <summary>
    /// setValue without params.value returns selector.invalid.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake setter registered
    ///
    /// Steps:
    /// - Handshake then setValue with automationId only
    ///
    /// Expected:
    /// - ok=false with selector.invalid
    /// </remarks>
    [Fact]
    public async Task SetValue_WithoutValue_ReturnsSelectorInvalid()
    {
        AgentServices.RegisterElementValueSetter(new FakeElementValueSetter());
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.SetValue,
            Params = JsonSerializer.SerializeToElement(new { automationId = "SampleTextBox" }),
        };
        await JsonMessageCodec.WriteRequestAsync(client, request);
        var response = await JsonMessageCodec.ReadResponseAsync(client);

        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.SelectorInvalid, response.Error?.Code);
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

        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        return await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
    }

    private static async Task<ResponseMessage> SendSetValueAsync(Stream stream, string automationId, string value)
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.SetValue,
            Params = JsonSerializer.SerializeToElement(new { automationId, value }),
        };

        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        return await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
    }

    private static void ClearGraftEnvironment()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, null);
    }

    private sealed class FakeElementValueSetter : IElementValueSetter
    {
        public string? LastAutomationId { get; private set; }

        public string? LastValue { get; private set; }

        public void SetValue(ElementSelector selector, string value)
        {
            LastAutomationId = selector.AutomationId;
            LastValue = value;
        }
    }
}
