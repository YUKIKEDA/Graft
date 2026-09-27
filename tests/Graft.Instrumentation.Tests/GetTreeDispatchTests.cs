using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;
using Graft.TestSupport;

namespace Graft.Instrumentation.Tests;

public sealed class GetTreeDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-gtd-" + Guid.NewGuid().ToString("N");

    public GetTreeDispatchTests()
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
    /// getTree without a registered provider returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; no tree provider is registered
    ///
    /// Steps:
    /// - Handshake then getTree
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task GetTree_WithoutProvider_ReturnsActionFailed()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, GetTreeRequest());
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// getTree returns the fake provider payload after handshake.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IUiTreeProvider registered
    ///
    /// Steps:
    /// - Handshake then getTree
    ///
    /// Expected:
    /// - ok=true and root.automationId matches the fake tree
    /// </remarks>
    [Fact]
    public async Task GetTree_WithFakeProvider_ReturnsRoot()
    {
        Agent.Use(new AgentBackend { TreeProvider = new FakeTreeProvider() });
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, GetTreeRequest());
        Assert.True(response.Ok, response.Error?.Message);
        Assert.True(response.Result.HasValue);

        var result = response.Result.Value.Deserialize<GetTreeResult>(JsonMessageCodec.Options);
        Assert.NotNull(result);
        Assert.Equal("SampleButton", result.Root.AutomationId);
        Assert.Equal("Click Me", result.Root.Name);
        Assert.False(result.Truncated);
    }

    private static RequestMessage GetTreeRequest() =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.GetTree,
            Params = JsonSerializer.SerializeToElement(new { depth = 25, maxNodes = 2000 }),
        };
}
