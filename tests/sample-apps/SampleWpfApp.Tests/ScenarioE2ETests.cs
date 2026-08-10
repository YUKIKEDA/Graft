using Graft.Core.Scenario;

namespace SampleWpfApp.Tests;

/// <summary>
/// Scenario JSON acceptance for SampleWpfApp (Phase 2 Batch 4).
/// </summary>
[Collection(SampleUiCollection.Name)]
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

    /// <summary>
    /// phase5-actions.scenario.json exercises scrollIntoView / select / expand / collapse.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase5-actions.scenario.json is copied to the test output
    /// - SampleWpfApp has SampleList / SampleTreeRoot / StatusText side effects
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override to the sample project
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase5Actions_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase5-actions.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase6-tree-state.scenario.json exercises expectSelected / expectExpanded.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase6-tree-state.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase6TreeState_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase6-tree-state.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase7-windows.scenario.json exercises list/wait/switch and invokeOpeningWindow.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase7-windows.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase7Windows_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase7-windows.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase8-datagrid.scenario.json exercises DataGrid row scroll/select and expectChecked.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase8-datagrid.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase8DataGrid_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase8-datagrid.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }
}
