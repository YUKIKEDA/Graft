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

public sealed class InvokeDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-inv-" + Guid.NewGuid().ToString("N");

    public InvokeDispatchTests()
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
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendInvokeAsync(client, "SampleButton");
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
        AgentServices.RegisterElementInvoker(fake);
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendInvokeAsync(client, "SampleButton");
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleButton", fake.LastAutomationId);
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
        AgentServices.RegisterElementInvoker(fake);
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendRightClickAsync(client, "ContextMenuTarget");
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
        AgentServices.RegisterElementInvoker(
            new FakeElementInvoker(throwCode: GraftErrorCodes.ElementNotFound)
        );
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendInvokeAsync(client, "Missing");
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ElementNotFound, response.Error?.Code);
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

    private static async Task<ResponseMessage> SendInvokeAsync(Stream stream, string automationId)
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.Invoke,
            Params = JsonSerializer.SerializeToElement(new { automationId }),
        };

        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        return await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
    }

    private static async Task<ResponseMessage> SendRightClickAsync(
        Stream stream,
        string automationId
    )
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "3",
            Method = ProtocolMethods.RightClick,
            Params = JsonSerializer.SerializeToElement(new { automationId }),
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

    private sealed class FakeElementInvoker : IElementInvoker
    {
        private readonly string? _throwCode;

        public FakeElementInvoker(string? throwCode = null) => _throwCode = throwCode;

        public string? LastAutomationId { get; private set; }

        public void Invoke(ElementSelector selector)
        {
            LastAutomationId = selector.AutomationId;
            if (_throwCode is not null)
            {
                throw new ElementResolveException(_throwCode, "fake failure");
            }
        }

        public void BeginInvoke(ElementSelector selector) => Invoke(selector);

        public void RightClick(ElementSelector selector) => Invoke(selector);
    }
}
