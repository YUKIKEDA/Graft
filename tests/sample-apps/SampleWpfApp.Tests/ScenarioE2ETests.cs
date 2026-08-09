using Graft.Core.Scenario;

namespace SampleWpfApp.Tests;

/// <summary>
/// Scenario JSON acceptance for SampleWpfApp (Phase 2 Batch 4).
/// </summary>
public sealed class ScenarioE2ETests
{
    /// <summary>
    /// sample-main-window.scenario.json launches the app, clicks SampleButton, expects StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/sample-main-window.scenario.json is copied to the test output
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override to the sample project
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task SampleMainWindow_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "sample-main-window.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }
}
