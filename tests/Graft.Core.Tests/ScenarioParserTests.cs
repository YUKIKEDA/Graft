using Graft.Core.Scenario;
using Graft.Protocol;

namespace Graft.Core.Tests;

public sealed class ScenarioParserTests
{
    /// <summary>
    /// Sample Scenario JSON compiles to launch / invoke / expectName operations.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fixtures/sample-main-window.scenario.json exists beside the test assembly
    ///
    /// Steps:
    /// - ScenarioJson.ParseFile on the fixture
    ///
    /// Expected:
    /// - Version 1, name sample-main-window
    /// - Operations: Launch(SampleWpfApp.csproj), Invoke(SampleButton), ExpectName(StatusText, Clicked 1)
    /// </remarks>
    [Fact]
    public void ParseFile_SampleFixture_CompilesOperations()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-main-window.scenario.json");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        var scenario = ScenarioJson.ParseFile(path);

        Assert.Equal(ScenarioDocument.CurrentVersion, scenario.Version);
        Assert.Equal("sample-main-window", scenario.Name);
        Assert.Equal(3, scenario.Operations.Count);

        var launch = Assert.IsType<LaunchOperation>(scenario.Operations[0]);
        Assert.Equal("SampleWpfApp.csproj", launch.AppPath);
        Assert.Equal("GraftTest", launch.Configuration);
        Assert.Equal(TimeSpan.FromSeconds(60), launch.Timeout);

        var invoke = Assert.IsType<InvokeOperation>(scenario.Operations[1]);
        Assert.Equal("SampleButton", invoke.Target.AutomationId);

        var expect = Assert.IsType<ExpectNameOperation>(scenario.Operations[2]);
        Assert.Equal("StatusText", expect.Target.AutomationId);
        Assert.Equal("Clicked 1", expect.Name);
    }

    /// <summary>
    /// setValue steps compile with automationId and value.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + setValue
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Second operation is SetValueOperation with matching fields
    /// </remarks>
    [Fact]
    public void Parse_SetValueStep_CompilesSetValueOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "setValue", "automationId": "SampleTextBox", "value": "hello-graft" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var setValue = Assert.IsType<SetValueOperation>(scenario.Operations[1]);
        Assert.Equal(ScenarioActions.SetValue, setValue.Action);
        Assert.Equal("SampleTextBox", setValue.Target.AutomationId);
        Assert.Equal("hello-graft", setValue.Value);
    }

    /// <summary>
    /// toggle / sendKeys steps compile with expected fields.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + toggle + sendKeys
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - ToggleOperation and SendKeysOperation with matching fields
    /// </remarks>
    [Fact]
    public void Parse_ToggleAndSendKeys_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "toggle", "automationId": "SampleCheckBox" },
                { "action": "sendKeys", "automationId": "SampleTextBox", "text": "abc" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var toggle = Assert.IsType<ToggleOperation>(scenario.Operations[1]);
        Assert.Equal("SampleCheckBox", toggle.Target.AutomationId);
        var sendKeys = Assert.IsType<SendKeysOperation>(scenario.Operations[2]);
        Assert.Equal("SampleTextBox", sendKeys.Target.AutomationId);
        Assert.Equal("abc", sendKeys.Text);
    }

    /// <summary>
    /// typeHuman compiles text and a non-negative delay.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + typeHuman
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - TypeHumanOperation with matching automationId, text, and delayMs
    /// </remarks>
    [Fact]
    public void Parse_TypeHumanStep_CompilesDelay()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "typeHuman", "automationId": "SampleTextBox", "text": "ab", "delayMs": 40 }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var typeHuman = Assert.IsType<TypeHumanOperation>(scenario.Operations[1]);
        Assert.Equal("SampleTextBox", typeHuman.Target.AutomationId);
        Assert.Equal("ab", typeHuman.Text);
        Assert.Equal(40, typeHuman.DelayMs);
    }

    /// <summary>
    /// rightClick steps compile with automationId.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + rightClick
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - RightClickOperation with matching automationId
    /// </remarks>
    [Fact]
    public void Parse_RightClickStep_CompilesRightClickOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "rightClick", "automationId": "ContextMenuTarget" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var rightClick = Assert.IsType<RightClickOperation>(scenario.Operations[1]);
        Assert.Equal(ScenarioActions.RightClick, rightClick.Action);
        Assert.Equal("ContextMenuTarget", rightClick.Target.AutomationId);
    }

    /// <summary>
    /// screenshot steps compile with a required path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + screenshot
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - ScreenshotOperation with matching path
    /// </remarks>
    [Fact]
    public void Parse_ScreenshotStep_CompilesScreenshotOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "screenshot", "path": "out/shot.png" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var shot = Assert.IsType<ScreenshotOperation>(scenario.Operations[1]);
        Assert.Equal(ScenarioActions.Screenshot, shot.Action);
        Assert.Equal("out/shot.png", shot.Path);
        Assert.Null(shot.Target);
    }

    /// <summary>
    /// screenshot steps compile with an optional automationId clip.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + screenshot automationId
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - ScreenshotOperation with path and automationId
    /// </remarks>
    [Fact]
    public void Parse_ScreenshotStepWithAutomationId_CompilesClip()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "screenshot", "path": "out/clip.png", "automationId": "SampleButton" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var shot = Assert.IsType<ScreenshotOperation>(scenario.Operations[1]);
        Assert.Equal("out/clip.png", shot.Path);
        Assert.NotNull(shot.Target);
        Assert.Equal("SampleButton", shot.Target.AutomationId);
    }

    /// <summary>
    /// pressKeys steps compile with automationId and keys.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + pressKeys
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - PressKeysOperation with matching fields
    /// </remarks>
    [Fact]
    public void Parse_PressKeysStep_CompilesPressKeysOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "pressKeys", "automationId": "SampleTextBox", "keys": "Control+A" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var press = Assert.IsType<PressKeysOperation>(scenario.Operations[1]);
        Assert.Equal(ScenarioActions.PressKeys, press.Action);
        Assert.Equal("SampleTextBox", press.Target.AutomationId);
        Assert.Equal("Control+A", press.Keys);
    }

    /// <summary>
    /// Phase 5 actions compile with expected fields (optional scroll index).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with scrollIntoView / select / expand / collapse
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types and field values
    /// </remarks>
    [Fact]
    public void Parse_Phase5Actions_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "scrollIntoView", "automationId": "SampleList", "index": 40 },
                { "action": "scrollIntoView", "automationId": "StatusText" },
                { "action": "select", "automationId": "SampleList", "index": 35 },
                { "action": "expand", "automationId": "SampleTreeRoot" },
                { "action": "collapse", "automationId": "SampleTreeRoot" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);

        var scrollIndexed = Assert.IsType<ScrollIntoViewOperation>(scenario.Operations[1]);
        Assert.Equal("SampleList", scrollIndexed.Target.AutomationId);
        Assert.Equal(40, scrollIndexed.Index);

        var scrollElement = Assert.IsType<ScrollIntoViewOperation>(scenario.Operations[2]);
        Assert.Equal("StatusText", scrollElement.Target.AutomationId);
        Assert.Null(scrollElement.Index);

        var select = Assert.IsType<SelectOperation>(scenario.Operations[3]);
        Assert.Equal("SampleList", select.Target.AutomationId);
        Assert.Equal(35, select.Index);

        var expand = Assert.IsType<ExpandOperation>(scenario.Operations[4]);
        Assert.Equal("SampleTreeRoot", expand.Target.AutomationId);

        var collapse = Assert.IsType<CollapseOperation>(scenario.Operations[5]);
        Assert.Equal("SampleTreeRoot", collapse.Target.AutomationId);
    }

    /// <summary>
    /// selectMany steps compile with automationId and indexes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + selectMany
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Second operation is SelectManyOperation with matching fields
    /// </remarks>
    [Fact]
    public void Parse_SelectManyStep_CompilesSelectManyOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "selectMany", "automationId": "SampleMultiList", "indexes": [1, 3] }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var selectMany = Assert.IsType<SelectManyOperation>(scenario.Operations[1]);
        Assert.Equal(ScenarioActions.SelectMany, selectMany.Action);
        Assert.Equal("SampleMultiList", selectMany.Target.AutomationId);
        Assert.Equal(new[] { 1, 3 }, selectMany.Indexes);
    }

    /// <summary>
    /// selectMenu steps compile with automationId and path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with launch + selectMenu
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Second operation is SelectMenuOperation with matching fields
    /// </remarks>
    [Fact]
    public void Parse_SelectMenuStep_CompilesSelectMenuOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                {
                  "action": "selectMenu",
                  "automationId": "SampleMenu",
                  "path": "SampleMenuFile/SampleMenuPing"
                }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var selectMenu = Assert.IsType<SelectMenuOperation>(scenario.Operations[1]);
        Assert.Equal(ScenarioActions.SelectMenu, selectMenu.Action);
        Assert.Equal("SampleMenu", selectMenu.Target.AutomationId);
        Assert.Equal("SampleMenuFile/SampleMenuPing", selectMenu.Path);
    }

    /// <summary>
    /// select with key and selectTree compile.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with select key + selectTree
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - SelectOperation.Key and SelectTreeOperation.Path match
    /// </remarks>
    [Fact]
    public void Parse_SelectKeyAndSelectTree_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "select", "automationId": "SampleList", "key": "Item 35" },
                {
                  "action": "selectTree",
                  "automationId": "SampleTree",
                  "path": "SampleTreeRoot/SampleTreeChildA"
                }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var select = Assert.IsType<SelectOperation>(scenario.Operations[1]);
        Assert.Equal("Item 35", select.Key);
        Assert.Null(select.Index);

        var selectTree = Assert.IsType<SelectTreeOperation>(scenario.Operations[2]);
        Assert.Equal("SampleTreeRoot/SampleTreeChildA", selectTree.Path);
    }

    /// <summary>
    /// Phase 28 DataGrid steps compile.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with selectCell / selectRow / clickColumnHeader / addRow / deleteSelectedRows
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types and fields
    /// </remarks>
    [Fact]
    public void Parse_Phase28DataGridSteps_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                {
                  "action": "selectCell",
                  "automationId": "Grid",
                  "row": 1,
                  "columnKey": "Name"
                },
                {
                  "action": "selectRow",
                  "automationId": "Grid",
                  "columnKey": "Name",
                  "value": "P28-5"
                },
                {
                  "action": "clickColumnHeader",
                  "automationId": "Grid",
                  "columnKey": "Name"
                },
                { "action": "addRow", "automationId": "Grid" },
                { "action": "deleteSelectedRows", "automationId": "Grid" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var selectCell = Assert.IsType<SelectCellOperation>(scenario.Operations[1]);
        Assert.Equal(1, selectCell.Row);
        Assert.Equal("Name", selectCell.ColumnKey);

        var selectRow = Assert.IsType<SelectRowOperation>(scenario.Operations[2]);
        Assert.Equal("P28-5", selectRow.Value);

        Assert.IsType<ClickColumnHeaderOperation>(scenario.Operations[3]);
        Assert.IsType<AddRowOperation>(scenario.Operations[4]);
        Assert.IsType<DeleteSelectedRowsOperation>(scenario.Operations[5]);
    }

    /// <summary>
    /// expectSelected / expectExpanded compile with boolean fields.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with expectSelected and expectExpanded
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types and bool values
    /// </remarks>
    [Fact]
    public void Parse_ExpectSelectedAndExpanded_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "expectSelected", "automationId": "ListItem-35", "selected": true },
                { "action": "expectExpanded", "automationId": "SampleTreeRoot", "expanded": false }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var selected = Assert.IsType<ExpectSelectedOperation>(scenario.Operations[1]);
        Assert.Equal("ListItem-35", selected.Target.AutomationId);
        Assert.True(selected.Selected);
        var expanded = Assert.IsType<ExpectExpandedOperation>(scenario.Operations[2]);
        Assert.Equal("SampleTreeRoot", expanded.Target.AutomationId);
        Assert.False(expanded.Expanded);
    }

    /// <summary>
    /// expectChecked compiles with boolean field.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with expectChecked
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - ExpectCheckedOperation with checked value
    /// </remarks>
    [Fact]
    public void Parse_ExpectChecked_CompileOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "expectChecked", "automationId": "SampleCheckBox", "checked": true }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var expectChecked = Assert.IsType<ExpectCheckedOperation>(scenario.Operations[1]);
        Assert.Equal("SampleCheckBox", expectChecked.Target.AutomationId);
        Assert.True(expectChecked.Checked);
    }

    /// <summary>
    /// expectFocused compiles with automationId only.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with expectFocused
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - ExpectFocusedOperation
    /// </remarks>
    [Fact]
    public void Parse_ExpectFocused_CompileOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "expectFocused", "automationId": "SamplePhase29aFocusB" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var expectFocused = Assert.IsType<ExpectFocusedOperation>(scenario.Operations[1]);
        Assert.Equal("SamplePhase29aFocusB", expectFocused.Target.AutomationId);
    }

    /// <summary>
    /// getCellText / setCellValue / expectCellText compile with row/column.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with cell actions
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types and indices
    /// </remarks>
    [Fact]
    public void Parse_CellTextActions_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "getCellText", "automationId": "SampleGrid", "row": 1, "column": 0 },
                { "action": "setCellValue", "automationId": "SampleGrid", "row": 2, "column": 0, "value": "x" },
                { "action": "expectCellText", "automationId": "SampleGrid", "row": 2, "column": 0, "text": "x" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var get = Assert.IsType<GetCellTextOperation>(scenario.Operations[1]);
        Assert.Equal("SampleGrid", get.Target.AutomationId);
        Assert.Equal(1, get.Row);
        Assert.Equal(0, get.Column);
        Assert.Null(get.ColumnKey);
        var set = Assert.IsType<SetCellValueOperation>(scenario.Operations[2]);
        Assert.Equal(2, set.Row);
        Assert.Equal("x", set.Value);
        var expect = Assert.IsType<ExpectCellTextOperation>(scenario.Operations[3]);
        Assert.Equal("x", expect.Text);
    }

    /// <summary>
    /// Cell actions compile with columnKey instead of column.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with columnKey cell actions
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operations with Column null and ColumnKey set
    /// </remarks>
    [Fact]
    public void Parse_CellTextActions_WithColumnKey_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "getCellText", "automationId": "SampleGrid", "row": 1, "columnKey": "Name" },
                { "action": "setCellValue", "automationId": "SampleGrid", "row": 2, "columnKey": "Active", "value": "True" },
                { "action": "expectCellText", "automationId": "SampleGrid", "row": 2, "columnKey": "Active", "text": "True" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var get = Assert.IsType<GetCellTextOperation>(scenario.Operations[1]);
        Assert.Null(get.Column);
        Assert.Equal("Name", get.ColumnKey);
        var set = Assert.IsType<SetCellValueOperation>(scenario.Operations[2]);
        Assert.Null(set.Column);
        Assert.Equal("Active", set.ColumnKey);
        Assert.Equal("True", set.Value);
        var expect = Assert.IsType<ExpectCellTextOperation>(scenario.Operations[3]);
        Assert.Equal("Active", expect.ColumnKey);
        Assert.Equal("True", expect.Text);
    }

    /// <summary>
    /// armOpenFile / armOpenFileCancel / invokeOpeningWindow waitForNewWindow compile.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with OpenFile arm actions
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types
    /// </remarks>
    [Fact]
    public void Parse_OpenFileArmActions_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "armOpenFile", "path": "C:\\a.txt" },
                { "action": "armOpenFileCancel" },
                { "action": "invokeOpeningWindow", "automationId": "OpenFileButton", "waitForNewWindow": false }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var arm = Assert.IsType<ArmOpenFileOperation>(scenario.Operations[1]);
        Assert.Equal(@"C:\a.txt", arm.Path);
        Assert.IsType<ArmOpenFileCancelOperation>(scenario.Operations[2]);
        var invoke = Assert.IsType<InvokeOpeningWindowOperation>(scenario.Operations[3]);
        Assert.False(invoke.WaitForNewWindow);
    }

    /// <summary>
    /// armSaveFile / armSaveFileCancel compile.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with SaveFile arm actions
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types
    /// </remarks>
    [Fact]
    public void Parse_SaveFileArmActions_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "armSaveFile", "path": "C:\\b.txt" },
                { "action": "armSaveFileCancel" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var arm = Assert.IsType<ArmSaveFileOperation>(scenario.Operations[1]);
        Assert.Equal(@"C:\b.txt", arm.Path);
        Assert.IsType<ArmSaveFileCancelOperation>(scenario.Operations[2]);
    }

    /// <summary>
    /// armOpenFolder / armOpenFolderCancel compile.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with OpenFolder arm actions
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types
    /// </remarks>
    [Fact]
    public void Parse_OpenFolderArmActions_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "armOpenFolder", "path": "C:\\folder" },
                { "action": "armOpenFolderCancel" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var arm = Assert.IsType<ArmOpenFolderOperation>(scenario.Operations[1]);
        Assert.Equal(@"C:\folder", arm.Path);
        Assert.IsType<ArmOpenFolderCancelOperation>(scenario.Operations[2]);
    }

    /// <summary>
    /// armMessageBox compiles.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Minimal JSON with armMessageBox
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - ArmMessageBoxOperation with result Yes
    /// </remarks>
    [Fact]
    public void Parse_MessageBoxArmAction_CompileOperation()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "armMessageBox", "result": "Yes" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var arm = Assert.IsType<ArmMessageBoxOperation>(scenario.Operations[1]);
        Assert.Equal("Yes", arm.Result);
    }

    /// <summary>
    /// Window actions compile to typed operations.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON with listWindows / waitForWindow / invokeOpeningWindow / switchWindow
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Matching operation types and field values
    /// </remarks>
    [Fact]
    public void Parse_WindowActions_CompileOperations()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "launch", "appPath": "App.csproj" },
                { "action": "listWindows" },
                { "action": "waitForWindow", "automationId": "ChildWindow", "switchTo": true },
                { "action": "invokeOpeningWindow", "automationId": "OpenModalWindowButton" },
                { "action": "switchWindow", "windowId": 2 }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        Assert.IsType<ListWindowsOperation>(scenario.Operations[1]);
        var wait = Assert.IsType<WaitForWindowOperation>(scenario.Operations[2]);
        Assert.Equal("ChildWindow", wait.AutomationId);
        Assert.True(wait.SwitchTo);
        var opening = Assert.IsType<InvokeOpeningWindowOperation>(scenario.Operations[3]);
        Assert.Equal("OpenModalWindowButton", opening.Target.AutomationId);
        var switchOp = Assert.IsType<SwitchWindowOperation>(scenario.Operations[4]);
        Assert.Equal(2, switchOp.WindowId);
    }

    /// <summary>
    /// Unknown action fails with action.failed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON with action "nope"
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - GraftException action.failed mentioning unknown action
    /// </remarks>
    [Fact]
    public void Parse_UnknownAction_ThrowsActionFailed()
    {
        const string json = """
            {
              "v": 1,
              "steps": [ { "action": "nope", "automationId": "x" } ]
            }
            """;

        var ex = Assert.Throws<GraftException>(() => ScenarioJson.Parse(json));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("unknown action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// expectNameContains rejects an empty substring.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON with expectNameContains and substring ""
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - GraftException action.failed mentioning non-empty substring
    /// </remarks>
    [Fact]
    public void Parse_ExpectNameContains_EmptySubstring_Throws()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                {
                  "action": "expectNameContains",
                  "automationId": "Status",
                  "substring": ""
                }
              ]
            }
            """;

        var ex = Assert.Throws<GraftException>(() => ScenarioJson.Parse(json));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("substring", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("non-empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A nested target compiles to the public selector fields.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON invoke step with target name and controlType
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - InvokeOperation.Target has that name and control type
    /// </remarks>
    [Fact]
    public void Parse_InvokeTarget_CompilesSelector()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "invoke", "target": { "name": "SampleClickMe", "controlType": "Button", "nth": 0 } }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var invoke = Assert.IsType<InvokeOperation>(scenario.Operations[0]);
        Assert.Equal("SampleClickMe", invoke.Target.Name);
        Assert.Equal("Button", invoke.Target.ControlType);
        Assert.Equal(0, invoke.Target.Nth);
    }

    /// <summary>
    /// The same side rejects both the short form and the target object.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON invoke step that sets automationId and target
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - GraftException action.failed mentioning both properties
    /// </remarks>
    [Fact]
    public void Parse_Invoke_BothForms_Throws()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "invoke", "automationId": "SampleButton", "target": { "name": "SampleClickMe" } }
              ]
            }
            """;

        var ex = Assert.Throws<GraftException>(() => ScenarioJson.Parse(json));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("both", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A required element step rejects a missing target.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON invoke step with only action
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - GraftException action.failed requiring automationId or target
    /// </remarks>
    [Fact]
    public void Parse_Invoke_NeitherForm_Throws()
    {
        const string json = """
            {
              "v": 1,
              "steps": [ { "action": "invoke" } ]
            }
            """;

        var ex = Assert.Throws<GraftException>(() => ScenarioJson.Parse(json));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("automationId", ex.Message, StringComparison.Ordinal);
        Assert.Contains("target", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A target with no string criterion is rejected.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON invoke step whose target sets only nth
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - GraftException action.failed requiring a string criterion
    /// </remarks>
    [Fact]
    public void Parse_Invoke_NthOnlyTarget_Throws()
    {
        const string json = """
            {
              "v": 1,
              "steps": [ { "action": "invoke", "target": { "nth": 0 } } ]
            }
            """;

        var ex = Assert.Throws<GraftException>(() => ScenarioJson.Parse(json));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("nearAutomationId", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Drag sides may mix the short form and the object.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - One drag uses automationId with to, and one uses target with toAutomationId
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - Each side keeps the form that was set
    /// </remarks>
    [Fact]
    public void Parse_Drag_MixedSides_Compile()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "drag", "automationId": "Source", "to": { "name": "Drop" } },
                { "action": "drag", "target": { "name": "Source" }, "toAutomationId": "DropTarget" }
              ]
            }
            """;

        var scenario = ScenarioJson.Parse(json);
        var first = Assert.IsType<DragOperation>(scenario.Operations[0]);
        Assert.Equal("Source", first.Target.AutomationId);
        Assert.Equal("Drop", first.To.Name);
        var second = Assert.IsType<DragOperation>(scenario.Operations[1]);
        Assert.Equal("Source", second.Target.Name);
        Assert.Equal("DropTarget", second.To.AutomationId);
    }

    /// <summary>
    /// A drag destination rejects both toAutomationId and to.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - JSON drag step that sets both destination forms
    ///
    /// Steps:
    /// - ScenarioJson.Parse
    ///
    /// Expected:
    /// - GraftException action.failed mentioning both properties
    /// </remarks>
    [Fact]
    public void Parse_Drag_BothDestinationForms_Throws()
    {
        const string json = """
            {
              "v": 1,
              "steps": [
                { "action": "drag", "automationId": "Source", "toAutomationId": "Drop", "to": { "name": "Drop" } }
              ]
            }
            """;

        var ex = Assert.Throws<GraftException>(() => ScenarioJson.Parse(json));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.Contains("both", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
