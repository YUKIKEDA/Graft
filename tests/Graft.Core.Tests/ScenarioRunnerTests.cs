using Graft.Core.Scenario;
using Graft.Protocol;

namespace Graft.Core.Tests;

public sealed class ScenarioRunnerTests
{
    /// <summary>
    /// Runner rejects scenarios that do not start with launch.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Document with only invoke
    ///
    /// Steps:
    /// - ScenarioRunner.RunAsync
    ///
    /// Expected:
    /// - GraftException action.failed about launch first
    /// </remarks>
    [Fact]
    public async Task RunAsync_WithoutLeadingLaunch_Throws()
    {
        var scenario = new ScenarioDocument { Version = ScenarioDocument.CurrentVersion, Operations = [new InvokeOperation("SampleButton")] };

        var ex = await Assert.ThrowsAsync<GraftException>(() => ScenarioRunner.RunAsync(scenario));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("launch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
