using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;
using Graft.TestSupport;

namespace Graft.Instrumentation.Tests;

public sealed class InvokeDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-inv-" + Guid.NewGuid().ToString("N");

    public InvokeDispatchTests()
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
    /// invoke without a registered invoker returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; ElementInvoker is null
    ///
    /// Steps:
    /// - Handshake then invoke
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task Invoke_WithoutInvoker_ReturnsActionFailed()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, TargetRequest(ProtocolMethods.Invoke, "SampleButton", "2"));
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// invoke dispatches to the registered invoker with the selector automationId.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementInvoker registered
    ///
    /// Steps:
    /// - Handshake then invoke SampleButton
    ///
    /// Expected:
    /// - ok=true and fake invoker received automationId SampleButton
    /// </remarks>
    [Fact]
    public async Task Invoke_WithFakeInvoker_CallsInvoke()
    {
        var fake = new FakeElementInvoker();
        Agent.Use(new AgentBackend { ElementInvoker = fake });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, TargetRequest(ProtocolMethods.Invoke, "SampleButton", "2"));
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleButton", fake.LastAutomationId);
    }

    /// <summary>
    /// invoke with a name and no automation id reaches the invoker.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementInvoker registered
    ///
    /// Steps:
    /// - Handshake then invoke with params name Save
    ///
    /// Expected:
    /// - ok=true and the fake invoker received name Save
    /// </remarks>
    [Fact]
    public async Task Invoke_WithName_CallsInvoke()
    {
        var fake = new FakeElementInvoker();
        Agent.Use(new AgentBackend { ElementInvoker = fake });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(
            client,
            new RequestMessage
            {
                V = ProtocolVersion.Current,
                Id = "9",
                Method = ProtocolMethods.Invoke,
                Params = JsonSerializer.SerializeToElement(new { name = "Save" }),
            }
        );
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("Save", fake.LastName);
        Assert.True(string.IsNullOrEmpty(fake.LastAutomationId));
    }

    /// <summary>
    /// rightClick dispatches to the registered invoker.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementInvoker registered
    ///
    /// Steps:
    /// - Handshake then rightClick
    ///
    /// Expected:
    /// - ok=true and fake received automationId
    /// </remarks>
    [Fact]
    public async Task RightClick_WithFakeInvoker_CallsRightClick()
    {
        var fake = new FakeElementInvoker();
        Agent.Use(new AgentBackend { ElementInvoker = fake });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, TargetRequest(ProtocolMethods.RightClick, "ContextMenuTarget", "3"));
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("ContextMenuTarget", fake.LastAutomationId);
    }

    /// <summary>
    /// invoke maps ElementResolveException codes onto the wire error.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake invoker throws element.notFound
    ///
    /// Steps:
    /// - Handshake then invoke
    ///
    /// Expected:
    /// - ok=false with element.notFound
    /// </remarks>
    [Fact]
    public async Task Invoke_WhenResolverFails_ReturnsElementNotFound()
    {
        Agent.Use(new AgentBackend { ElementInvoker = new FakeElementInvoker(throwCode: GraftErrorCodes.ElementNotFound) });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, TargetRequest(ProtocolMethods.Invoke, "Missing", "2"));
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ElementNotFound, response.Error?.Code);
    }

    private static RequestMessage TargetRequest(string method, string automationId, string id) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Method = method,
            Params = JsonSerializer.SerializeToElement(new { automationId }),
        };
}
