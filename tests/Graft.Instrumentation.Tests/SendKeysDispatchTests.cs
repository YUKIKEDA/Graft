using System.Text.Json;
using Graft.Instrumentation;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class SendKeysDispatchTests : IDisposable
{
    private readonly string _pipeName = "graft-keys-" + Guid.NewGuid().ToString("N");

    public SendKeysDispatchTests()
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
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, SendKeysRequest("SampleTextBox", "abc"));
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
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, SendKeysRequest("SampleTextBox", "typed"));
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleTextBox", fake.LastAutomationId);
        Assert.Equal("typed", fake.LastText);
    }

    /// <summary>
    /// typeHuman without a registered key sender returns action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started; ElementKeySender is null
    ///
    /// Steps:
    /// - Handshake then typeHuman
    ///
    /// Expected:
    /// - ok=false with action.failed
    /// </remarks>
    [Fact]
    public async Task TypeHuman_WithoutSender_ReturnsActionFailed()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, TypeHumanRequest("SampleTextBox", "ab", 40));
        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ActionFailed, response.Error?.Code);
    }

    /// <summary>
    /// typeHuman dispatches text and delay, and rejects a negative delay.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementKeySender registered
    ///
    /// Steps:
    /// - Handshake then typeHuman with delayMs 40
    /// - typeHuman with delayMs -1
    ///
    /// Expected:
    /// - the first call is ok and the fake records text and 40ms
    /// - the second call is selector.invalid and does not replace the recorded call
    /// </remarks>
    [Fact]
    public async Task TypeHuman_WithFakeSender_PassesDelay_AndRejectsNegative()
    {
        var fake = new FakeElementKeySender();
        AgentServices.RegisterElementKeySender(fake);
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, TypeHumanRequest("SampleTextBox", "ab", 40));
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleTextBox", fake.LastTypeAutomationId);
        Assert.Equal("ab", fake.LastTypeText);
        Assert.Equal(TimeSpan.FromMilliseconds(40), fake.LastDelay);

        var rejected = await PipeTestClient.SendAsync(client, TypeHumanRequest("SampleTextBox", "nope", -1));
        Assert.False(rejected.Ok);
        Assert.Equal(GraftErrorCodes.SelectorInvalid, rejected.Error?.Code);
        Assert.Equal("ab", fake.LastTypeText);
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
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, PressKeysRequest("SampleTextBox", "Control+A"));
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
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        Assert.True((await PipeTestClient.HandshakeAsync(client)).Ok);

        var response = await PipeTestClient.SendAsync(client, PressKeysRequest("SampleTextBox", "Control+A"));
        Assert.True(response.Ok, response.Error?.Message);
        Assert.Equal("SampleTextBox", fake.LastPressAutomationId);
        Assert.Equal("Control+A", fake.LastKeys);
    }

    private static RequestMessage SendKeysRequest(string automationId, string text) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "2",
            Method = ProtocolMethods.SendKeys,
            Params = JsonSerializer.SerializeToElement(new { automationId, text }),
        };

    private static RequestMessage TypeHumanRequest(string automationId, string text, int delayMs) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "4",
            Method = ProtocolMethods.TypeHuman,
            Params = JsonSerializer.SerializeToElement(
                new
                {
                    automationId,
                    text,
                    delayMs,
                }
            ),
        };

    private static RequestMessage PressKeysRequest(string automationId, string keys) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = "3",
            Method = ProtocolMethods.PressKeys,
            Params = JsonSerializer.SerializeToElement(new { automationId, keys }),
        };

    private sealed class FakeElementKeySender : IElementKeySender
    {
        public string? LastAutomationId { get; private set; }

        public string? LastText { get; private set; }

        public string? LastTypeAutomationId { get; private set; }

        public string? LastTypeText { get; private set; }

        public TimeSpan? LastDelay { get; private set; }

        public string? LastPressAutomationId { get; private set; }

        public string? LastKeys { get; private set; }

        public void SendKeys(ElementSelector selector, string text)
        {
            LastAutomationId = selector.AutomationId;
            LastText = text;
        }

        public void TypeHuman(ElementSelector selector, string text, TimeSpan delay)
        {
            LastTypeAutomationId = selector.AutomationId;
            LastTypeText = text;
            LastDelay = delay;
        }

        public void PressKeys(ElementSelector selector, string keys)
        {
            LastPressAutomationId = selector.AutomationId;
            LastKeys = keys;
        }
    }
}
