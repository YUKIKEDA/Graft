using System.Text.Json;
using Graft.McpServer.Tools;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Graft.McpServer.Tests;

[Collection(McpUiCollection.Name)]
public sealed class McpScenarioRunTests
{
    /// <summary>
    /// Invalid scenario input returns IsError with a stable code (no UI launch).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Neither scenarioJson nor scenarioPath provided
    ///
    /// Steps:
    /// - Call GraftRunScenarioTool.RunScenario
    ///
    /// Expected:
    /// - IsError true; JSON ok=false and code action.failed
    /// </remarks>
    [Fact]
    public async Task RunScenario_MissingInput_ReturnsErrorJson()
    {
        var result = await GraftRunScenarioTool.RunScenario();
        Assert.True(result.IsError);
        var text = GetText(result);
        using var doc = JsonDocument.Parse(text);
        Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("action.failed", doc.RootElement.GetProperty("code").GetString());
    }

    /// <summary>
    /// MCP client runs sample-main-window.scenario.json against SampleWpfApp.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fixtures/sample-main-window.scenario.json copied to output
    /// - SampleWpfApp.csproj can build with GraftTest
    ///
    /// Steps:
    /// - CallToolAsync graft_run_scenario with scenarioPath + appPath
    ///
    /// Expected:
    /// - IsError false; JSON ok=true and name sample-main-window
    /// </remarks>
    [Fact]
    [Trait("Category", "UI")]
    public async Task RunScenario_SampleMainWindow_ViaMcp_Succeeds()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-main-window.scenario.json");
        Assert.True(File.Exists(scenarioPath), $"Missing fixture: {scenarioPath}");

        var serverDll = Path.Combine(AppContext.BaseDirectory, "Graft.McpServer.dll");
        Assert.True(File.Exists(serverDll), $"Missing server assembly: {serverDll}");

        var transport = new StdioClientTransport(
            new StdioClientTransportOptions
            {
                Name = "Graft.McpServer",
                Command = "dotnet",
                Arguments = ["exec", serverDll],
            }
        );

        await using var client = await McpClient.CreateAsync(transport);

        var result = await client.CallToolAsync(
            "graft_run_scenario",
            new Dictionary<string, object?> { ["scenarioPath"] = scenarioPath, ["appPath"] = SampleAppLocator.ResolveProjectPath() },
            cancellationToken: CancellationToken.None
        );

        Assert.False(result.IsError);
        var text = GetText(result);
        using var doc = JsonDocument.Parse(text);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("sample-main-window", doc.RootElement.GetProperty("name").GetString());
    }

    private static string GetText(CallToolResult result) => string.Join(string.Empty, result.Content.OfType<TextContentBlock>().Select(b => b.Text));
}
