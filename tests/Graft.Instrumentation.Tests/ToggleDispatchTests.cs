using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class ToggleDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-tog-" + Guid.NewGuid().ToString("N");

    public ToggleDispatchTests()
    {
        PipeTestClient.ClearEnvironment();
        Agent.Stop();
        AgentServices.Reset();
    }

    public void Dispose()
    {
        Agent.Stop();
        AgentServices.Reset();
        PipeTestClient.ClearEnvironment();
    }

    /// <summary>
    /// toggle without a registered toggler returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; ElementToggler is null
    ///
    /// Steps:
    /// - Handshake then toggle
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task Toggle_WithoutToggler_ReturnsActionFailed()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, ToggleRequest("SampleCheckBox"));
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// toggle dispatches to the registered toggler.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementToggler registered
    ///
    /// Steps:
    /// - Handshake then toggle SampleCheckBox
    ///
    /// Expected:
    /// - ok=true and fake received automationId SampleCheckBox
    /// </remarks>
    [Fact]
    public async Task Toggle_WithFakeToggler_CallsToggle()
    {
        var fake = new FakeElementToggler();
        AgentServices.RegisterElementToggler(fake);
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, ToggleRequest("SampleCheckBox"));
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleCheckBox", fake.LastAutomationId);
    }

    private static RequestMessage ToggleRequest(string automationId) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.Toggle,
            Params = JsonSerializer.SerializeToElement(new { automationId }),
        };

    private sealed class FakeElementToggler : IElementToggler
    {
        public string? LastAutomationId { get; private set; }

        public void Toggle(ElementSelector selector) => LastAutomationId = selector.AutomationId;
    }
}
