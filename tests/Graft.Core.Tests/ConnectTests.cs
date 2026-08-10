using Graft.Instrumentation;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

public sealed class ConnectTests : IDisposable
{
    private const string Token = "secret";

    private readonly string _pipeName = "graft-core-" + Guid.NewGuid().ToString("N");

    public ConnectTests()
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
    /// ConnectAsync handshakes and getTree returns the fake tree root.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with FakeTreeProvider
    ///
    /// Steps:
    /// - Application.ConnectAsync then GetTreeAsync
    ///
    /// Expected:
    /// - Root AutomationId is SampleButton
    /// </remarks>
    [Fact]
    public async Task Connect_ThenGetTree_ReturnsFakeRoot()
    {
        AgentServices.RegisterTreeProvider(new FakeTreeProvider());
        StartAgent();

        await using var connection = await Application.ConnectAsync(
            _pipeName,
            Token,
            TimeSpan.FromSeconds(5)
        );
        var tree = await connection.GetTreeAsync();

        Assert.Equal("SampleButton", tree.Root.AutomationId);
        Assert.Equal("Click Me", tree.Root.Name);
    }

    /// <summary>
    /// ConnectAsync with a wrong token throws handshake.rejected.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with token "secret"
    ///
    /// Steps:
    /// - ConnectAsync with token "wrong"
    ///
    /// Expected:
    /// - GraftException with handshake.rejected
    /// </remarks>
    [Fact]
    public async Task Connect_WithWrongToken_ThrowsHandshakeRejected()
    {
        StartAgent();

        var ex = await Assert.ThrowsAsync<GraftException>(() =>
            Application.ConnectAsync(_pipeName, "wrong", TimeSpan.FromSeconds(5))
        );
        Assert.Equal(GraftErrorCodes.HandshakeRejected, ex.Code);
    }

    /// <summary>
    /// invoke after Connect dispatches to the registered invoker.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementInvoker registered; Agent started
    ///
    /// Steps:
    /// - ConnectAsync then InvokeAsync(SampleButton)
    ///
    /// Expected:
    /// - Fake received automationId SampleButton
    /// </remarks>
    [Fact]
    public async Task Connect_ThenInvoke_CallsFakeInvoker()
    {
        var fake = new FakeElementInvoker();
        AgentServices.RegisterElementInvoker(fake);
        StartAgent();

        await using var connection = await Application.ConnectAsync(
            _pipeName,
            Token,
            TimeSpan.FromSeconds(5)
        );
        await connection.InvokeAsync("SampleButton");

        Assert.Equal("SampleButton", fake.LastAutomationId);
    }

    /// <summary>
    /// setValue after Connect dispatches to the registered value setter.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake IElementValueSetter registered; Agent started
    ///
    /// Steps:
    /// - ConnectAsync then SetValueAsync(SampleTextBox, hello)
    ///
    /// Expected:
    /// - Fake received automationId SampleTextBox and value hello
    /// </remarks>
    [Fact]
    public async Task Connect_ThenSetValue_CallsFakeValueSetter()
    {
        var fake = new FakeElementValueSetter();
        AgentServices.RegisterElementValueSetter(fake);
        StartAgent();

        await using var connection = await Application.ConnectAsync(
            _pipeName,
            Token,
            TimeSpan.FromSeconds(5)
        );
        await connection.SetValueAsync("SampleTextBox", "hello");

        Assert.Equal("SampleTextBox", fake.LastAutomationId);
        Assert.Equal("hello", fake.LastValue);
    }

    private void StartAgent()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, _pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, Token);
        Agent.Start();
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

    private sealed class FakeElementInvoker : IElementInvoker
    {
        public string? LastAutomationId { get; private set; }

        public void Invoke(ElementSelector selector) => LastAutomationId = selector.AutomationId;

        public void BeginInvoke(ElementSelector selector) => Invoke(selector);

        public void RightClick(ElementSelector selector) => Invoke(selector);
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
