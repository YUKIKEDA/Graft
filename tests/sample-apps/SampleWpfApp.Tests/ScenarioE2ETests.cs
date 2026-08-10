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

    /// <summary>
    /// phase9-cell-rw.scenario.json exercises getCellText / setCellValue / expectCellText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase9-cell-rw.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase9CellRw_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase9-cell-rw.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase10-openfile.scenario.json exercises armOpenFile / armOpenFileCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase10-openfile.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase10OpenFile_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase10-openfile.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase11-savefile.scenario.json exercises armSaveFile / armSaveFileCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase11-savefile.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase11SaveFile_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase11-savefile.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase12-openfolder.scenario.json exercises armOpenFolder / armOpenFolderCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase12-openfolder.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase12OpenFolder_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase12-openfolder.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase13-messagebox.scenario.json exercises armMessageBox.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase13-messagebox.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase13MessageBox_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase13-messagebox.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }

    /// <summary>
    /// phase15-screenshot.scenario.json writes a PNG to the scenario path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase15-screenshot.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    /// - Read Artifacts/phase15-screenshot.png
    ///
    /// Expected:
    /// - File exists with PNG signature
    /// </remarks>
    [Fact]
    public async Task Phase15Screenshot_Scenario_WritesPng()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase15-screenshot.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var outPath = Path.Combine(AppContext.BaseDirectory, "Artifacts", "phase15-screenshot.png");
        if (File.Exists(outPath))
        {
            File.Delete(outPath);
        }

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        var previousCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            await ScenarioRunner.RunAsync(
                scenario,
                new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
            );
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
        }

        Assert.True(File.Exists(outPath), $"Missing screenshot: {outPath}");
        var bytes = await File.ReadAllBytesAsync(outPath);
        Assert.True(bytes.Length >= 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    /// <summary>
    /// phase14-press-keys.scenario.json clears SampleTextBox via Control+A / Delete.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/phase14-press-keys.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase14PressKeys_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scenarios",
            "phase14-press-keys.scenario.json"
        );
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(
            scenario,
            new ScenarioRunOptions { AppPath = SampleAppLocator.ResolveProjectPath() }
        );
    }
}
