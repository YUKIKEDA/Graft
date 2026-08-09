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

public sealed class ToggleDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-tog-" + Guid.NewGuid().ToString("N");

    public ToggleDispatchTests()
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
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendToggleAsync(client, "SampleCheckBox");
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
        StartAgent();

        await using var client = await ConnectAsync(_pipeName);
        Assert.True((await SendHandshakeAsync(client)).Ok);

        var response = await SendToggleAsync(client, "SampleCheckBox");
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleCheckBox", fake.LastAutomationId);
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
        await JsonMessageCodec.WriteRequestAsync(stream, request);
        return await JsonMessageCodec.ReadResponseAsync(stream);
    }

    private static async Task<ResponseMessage> SendToggleAsync(Stream stream, string automationId)
    {
        var request = new RequestMessage
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.Toggle,
            Params = JsonSerializer.SerializeToElement(new { automationId }),
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

    private sealed class FakeElementToggler : IElementToggler
    {
        public string? LastAutomationId { get; private set; }

        public void Toggle(ElementSelector selector) => LastAutomationId = selector.AutomationId;
    }
}
