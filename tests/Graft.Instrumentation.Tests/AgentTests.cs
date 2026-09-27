using Graft.Instrumentation;

namespace Graft.Instrumentation.Tests;

public sealed class AgentTests : IDisposable
{
    public AgentTests()
    {
        PipeTestClient.ClearEnvironment();
        Agent.Stop();
    }

    public void Dispose()
    {
        Agent.Stop();
        PipeTestClient.ClearEnvironment();
    }

    /// <summary>
    /// Start is a no-op when GRAFT_ENABLE is not set.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_ENABLE unset
    /// - Agent not running
    ///
    /// Steps:
    /// - Call Agent.Start
    ///
    /// Expected:
    /// - IsRunning is false and Current is null
    /// </remarks>
    [Fact]
    public void Start_WithoutEnableFlag_DoesNotStart()
    {
        Agent.Start();

        Assert.False(Agent.IsRunning);
        Assert.Null(Agent.Current);
    }

    /// <summary>
    /// Start requires GRAFT_PIPE_NAME when enabled.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_ENABLE=1
    /// - GRAFT_PIPE_NAME unset
    ///
    /// Steps:
    /// - Call Agent.Start
    ///
    /// Expected:
    /// - Throws InvalidOperationException
    /// </remarks>
    [Fact]
    public void Start_WithEnableButMissingPipeName_Throws()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");

        var ex = Assert.Throws<InvalidOperationException>(Agent.Start);
        Assert.Contains(GraftEnvironment.PipeName, ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Start refuses to listen without a connect token (#83).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_ENABLE=1 and GRAFT_PIPE_NAME set
    /// - GRAFT_CONNECT_TOKEN unset, then whitespace
    ///
    /// Steps:
    /// - Call Agent.Start for each token value
    ///
    /// Expected:
    /// - Throws InvalidOperationException naming GRAFT_CONNECT_TOKEN; agent is not running
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Start_WithEnableButMissingToken_Throws(string? token)
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, "graft-gate-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, token);

        var ex = Assert.Throws<InvalidOperationException>(Agent.Start);
        Assert.Contains(GraftEnvironment.ConnectToken, ex.Message, StringComparison.Ordinal);
        Assert.False(Agent.IsRunning);
    }

    /// <summary>
    /// Start captures pipe name and token when enabled.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_ENABLE=1
    /// - GRAFT_PIPE_NAME and GRAFT_CONNECT_TOKEN set
    ///
    /// Steps:
    /// - Call Agent.Start
    ///
    /// Expected:
    /// - IsRunning is true; Current exposes pipe name and token
    /// </remarks>
    [Fact]
    public void Start_WithEnableAndPipeName_SetsCurrentSession()
    {
        var pipeName = "graft-gate-" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, "secret");

        Agent.Start();

        Assert.True(Agent.IsRunning);
        Assert.NotNull(Agent.Current);
        Assert.Equal(pipeName, Agent.Current.PipeName);
        Assert.Equal("secret", Agent.Current.ConnectToken);
    }

    /// <summary>
    /// Stop clears the active session.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with enable + pipe name + token
    ///
    /// Steps:
    /// - Call Agent.Stop
    ///
    /// Expected:
    /// - IsRunning is false and Current is null
    /// </remarks>
    [Fact]
    public void Stop_AfterStart_ClearsSession()
    {
        var pipeName = "graft-gate-" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, "secret");
        Agent.Start();

        Agent.Stop();

        Assert.False(Agent.IsRunning);
        Assert.Null(Agent.Current);
    }
}
