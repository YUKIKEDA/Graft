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
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "sample-main-window.scenario.json"
        );
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
        Assert.Equal("SampleButton", invoke.AutomationId);

        var expect = Assert.IsType<ExpectNameOperation>(scenario.Operations[2]);
        Assert.Equal("StatusText", expect.AutomationId);
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
        Assert.Equal("SampleTextBox", setValue.AutomationId);
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
        Assert.Equal("SampleCheckBox", toggle.AutomationId);
        var sendKeys = Assert.IsType<SendKeysOperation>(scenario.Operations[2]);
        Assert.Equal("SampleTextBox", sendKeys.AutomationId);
        Assert.Equal("abc", sendKeys.Text);
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
        Assert.Equal("SampleList", scrollIndexed.AutomationId);
        Assert.Equal(40, scrollIndexed.Index);

        var scrollElement = Assert.IsType<ScrollIntoViewOperation>(scenario.Operations[2]);
        Assert.Equal("StatusText", scrollElement.AutomationId);
        Assert.Null(scrollElement.Index);

        var select = Assert.IsType<SelectOperation>(scenario.Operations[3]);
        Assert.Equal("SampleList", select.AutomationId);
        Assert.Equal(35, select.Index);

        var expand = Assert.IsType<ExpandOperation>(scenario.Operations[4]);
        Assert.Equal("SampleTreeRoot", expand.AutomationId);

        var collapse = Assert.IsType<CollapseOperation>(scenario.Operations[5]);
        Assert.Equal("SampleTreeRoot", collapse.AutomationId);
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
        Assert.Equal("ListItem-35", selected.AutomationId);
        Assert.True(selected.Selected);
        var expanded = Assert.IsType<ExpectExpandedOperation>(scenario.Operations[2]);
        Assert.Equal("SampleTreeRoot", expanded.AutomationId);
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
        Assert.Equal("SampleCheckBox", expectChecked.AutomationId);
        Assert.True(expectChecked.Checked);
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
        Assert.Equal("SampleGrid", get.AutomationId);
        Assert.Equal(1, get.Row);
        Assert.Equal(0, get.Column);
        var set = Assert.IsType<SetCellValueOperation>(scenario.Operations[2]);
        Assert.Equal(2, set.Row);
        Assert.Equal("x", set.Value);
        var expect = Assert.IsType<ExpectCellTextOperation>(scenario.Operations[3]);
        Assert.Equal("x", expect.Text);
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
        Assert.Equal("OpenModalWindowButton", opening.AutomationId);
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
}
