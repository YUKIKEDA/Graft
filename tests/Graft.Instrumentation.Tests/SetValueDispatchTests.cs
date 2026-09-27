using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;
using Graft.TestSupport;

namespace Graft.Instrumentation.Tests;

public sealed class SetValueDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-sv-" + Guid.NewGuid().ToString("N");

    public SetValueDispatchTests()
    {
        PipeTestClient.ClearEnvironment();
        Agent.Stop();
        Agent.Reset();
    }

    public void Dispose()
    {
        Agent.Stop();
        Agent.Reset();
        PipeTestClient.ClearEnvironment();
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
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, SetValueRequest("SampleTextBox", "x"));
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
        Agent.Use(new AgentBackend { ElementValueSetter = fake });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, SetValueRequest("SampleTextBox", "hello"));
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
        Agent.Use(new AgentBackend { ElementValueSetter = new FakeElementValueSetter() });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(
            client,
            new RequestMessage
            {
                V = ProtocolVersion.Current,
                Id = "2",
                Method = ProtocolMethods.SetValue,
                Params = JsonSerializer.SerializeToElement(new { automationId = "SampleTextBox" }),
            }
        );

        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.SelectorInvalid, response.Error?.Code);
    }

    private static RequestMessage SetValueRequest(string automationId, string value) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.SetValue,
            Params = JsonSerializer.SerializeToElement(new { automationId, value }),
        };
}
