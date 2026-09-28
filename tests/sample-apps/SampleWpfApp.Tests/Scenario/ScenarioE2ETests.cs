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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "sample-main-window.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// A nested target invokes SampleClickMe and expects StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - SampleButton exposes the accessible name SampleClickMe
    ///
    /// Steps:
    /// - Parse an inline scenario that invokes by name and control type
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Invoke_ByName_Passes()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "SampleWpfApp.csproj" },
                { "action": "invoke", "target": { "name": "SampleClickMe", "controlType": "Button" } },
                { "action": "expectName", "automationId": "StatusText", "name": "Clicked 1" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// actions.scenario.json exercises scrollIntoView / select / expand / collapse.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/actions.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "actions.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// tree-state.scenario.json exercises expectSelected / expectExpanded.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/tree-state.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "tree-state.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// windows.scenario.json exercises list/wait/switch and invokeOpeningWindow.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/windows.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "windows.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// datagrid-rows.scenario.json exercises DataGrid row scroll/select and expectChecked.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/datagrid-rows.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "datagrid-rows.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// cell-read-write.scenario.json exercises getCellText / setCellValue / expectCellText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/cell-read-write.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "cell-read-write.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// open-file.scenario.json exercises armOpenFile / armOpenFileCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/open-file.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "open-file.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// save-file.scenario.json exercises armSaveFile / armSaveFileCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/save-file.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "save-file.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// open-folder.scenario.json exercises armOpenFolder / armOpenFolderCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/open-folder.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "open-folder.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// message-box.scenario.json exercises armMessageBox.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/message-box.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "message-box.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// tab-control.scenario.json selects a TabControl tab by index.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/tab-control.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase17TabControl_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "tab-control.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// slider.scenario.json sets SampleSlider via setValue.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/slider.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase18Slider_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "slider.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// select-many.scenario.json multi-selects SampleMultiList.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/select-many.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase19SelectMany_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "select-many.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// menu-bar.scenario.json invokes File → Ping on the Menu bar.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/menu-bar.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase20MenuBar_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "menu-bar.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// datagrid-column-key.scenario.json uses columnKey for Name/Active cells.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/datagrid-column-key.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase21DataGridColumnKey_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "datagrid-column-key.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// datagrid-select-many.scenario.json multi-selects SampleMultiGrid rows.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/datagrid-select-many.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase22DataGridSelectMany_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "datagrid-select-many.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// wait-expect.scenario.json exercises Wait/Expect/value and window closed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/wait-expect.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase24WaitExpect_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "wait-expect.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// mouse.scenario.json exercises doubleClick / hover / drag / clickAt / wheel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/mouse.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase25Mouse_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "mouse.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// menu-depth.scenario.json exercises selectMenu on Menu and ContextMenu.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/menu-depth.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase26MenuDepth_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "menu-depth.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// selectors.scenario.json exercises select key and selectTree.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/selectors.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase27Selectors_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "selectors.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// datagrid-edit.scenario.json exercises Template/SelectCell/SelectRow/CRUD.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/datagrid-edit.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase28DataGrid_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "datagrid-edit.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// keyboard-controls.scenario.json exercises Password/RichText/Radio/focus/F5.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/keyboard-controls.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase29aControls_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "keyboard-controls.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// popup-controls.scenario.json exercises DatePicker/Combo/ListView/ToolTip/Popup/Hyperlink.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/popup-controls.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase29bControls_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "popup-controls.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// context-menu.scenario.json right-clicks and invokes a MenuItem.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/context-menu.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    ///
    /// Expected:
    /// - Scenario completes without GraftException
    /// </remarks>
    [Fact]
    public async Task Phase16ContextMenu_Scenario_Passes()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "context-menu.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }

    /// <summary>
    /// screenshot.scenario.json writes a PNG to the scenario path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/screenshot.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "screenshot.scenario.json");
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
            await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
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
    /// element-screenshot.scenario.json writes a clipped PNG of SampleButton.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/element-screenshot.scenario.json is copied to the test output
    ///
    /// Steps:
    /// - Parse Scenario JSON
    /// - ScenarioRunner.RunAsync with AppPath override
    /// - Read Artifacts/phase35-element-screenshot.png
    ///
    /// Expected:
    /// - File exists with PNG signature
    /// </remarks>
    [Fact]
    public async Task Phase35ElementScreenshot_Scenario_WritesPng()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "element-screenshot.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var outPath = Path.Combine(AppContext.BaseDirectory, "Artifacts", "phase35-element-screenshot.png");
        if (File.Exists(outPath))
        {
            File.Delete(outPath);
        }

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        var previousCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
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
    /// press-keys.scenario.json clears SampleTextBox via Control+A / Delete.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Scenarios/press-keys.scenario.json is copied to the test output
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
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "press-keys.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing scenario: {scenarioPath}");

        var scenario = ScenarioJson.ParseFile(scenarioPath);
        await ScenarioRunner.RunAsync(scenario, new ScenarioRunOptions { AppPath = SampleWpfLaunch.AppPath });
    }
}
