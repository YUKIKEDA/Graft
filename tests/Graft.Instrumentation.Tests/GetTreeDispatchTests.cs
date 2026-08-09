using System.IO.Pipes;
using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class GetTreeDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-gtd-" + Guid.NewGuid().ToString("N");

    public GetTreeDispatchTests()
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
    /// getTree without a registered provider returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; AgentServices.TreeProvider is null
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
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendGetTreeAsync(client);
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
        AgentServices.RegisterTreeProvider(new FakeTreeProvider());
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendGetTreeAsync(client);
        Assert.True(response.Ok, response.Error?.Message);
        Assert.True(response.Result.HasValue);

        var result = response.Result.Value.Deserialize<GetTreeResult>(JsonMessageCodec.Options);
        Assert.NotNull(result);
        Assert.Equal("SampleButton", result.Root.AutomationId);
        Assert.Equal("Click Me", result.Root.Name);
        Assert.False(result.Truncated);
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
        var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await client.ConnectAsync(200).ConfigureAwait(false);
                return client;
            }
            catch (Exception ex)
                when (ex is TimeoutException or IOException or UnauthorizedAccessException)
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

    private static async Task<ResponseMessage> SendGetTreeAsync(Stream stream)
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.GetTree,
            Params = JsonSerializer.SerializeToElement(new { depth = 25, maxNodes = 2000 }),
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

    private sealed class FakeTreeProvider : IUiTreeProvider
    {
        public GetTreeResult GetTree(GetTreeOptions options) =>
            new()
            {
                Truncated = false,
                Root = new TreeNode
                {
                    RuntimeId = 1,
                    ControlType = "Button",
                    Name = "Click Me",
                    AutomationId = "SampleButton",
                    Bounds = new ElementBounds
                    {
                        X = 10,
                        Y = 20,
                        Width = 80,
                        Height = 24,
                    },
                    Enabled = true,
                    Visible = true,
                    Focused = false,
                    Children = Array.Empty<TreeNode>(),
                },
            };
    }
}
