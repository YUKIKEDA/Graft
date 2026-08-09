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
