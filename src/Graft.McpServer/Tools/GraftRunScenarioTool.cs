using System.ComponentModel;
using System.Text.Json.Nodes;
using Graft.Core;
using Graft.Core.Scenario;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Graft.McpServer.Tools;

/// <summary>
/// MCP tool that runs a Graft Scenario JSON document via <see cref="ScenarioRunner"/>.
/// </summary>
[McpServerToolType]
public static class GraftRunScenarioTool
{
    /// <summary>
    /// Parses and runs a Scenario (JSON text or file path).
    /// </summary>
    /// <param name="scenarioJson">Scenario JSON text (exclusive with <paramref name="scenarioPath"/>).</param>
    /// <param name="scenarioPath">Path to a Scenario JSON file (exclusive with <paramref name="scenarioJson"/>).</param>
    /// <param name="appPath">Optional absolute app/csproj path overriding <c>launch.appPath</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="CallToolResult"/> with JSON text. Failures set <see cref="CallToolResult.IsError"/>
    /// and include <c>code</c> / <c>message</c> / optional <c>report</c> (FailureReport).
    /// </returns>
    [McpServerTool(Name = "graft_run_scenario")]
    [Description(
        "Run a Graft Scenario JSON (launch/invoke/setValue/expectName). Provide either scenarioJson or scenarioPath. Optional appPath overrides launch.appPath."
    )]
    public static async Task<CallToolResult> RunScenario(
        [Description("Scenario JSON document text.")] string? scenarioJson = null,
        [Description("Path to a Scenario .json file.")] string? scenarioPath = null,
        [Description("Absolute app/csproj path overriding launch.appPath.")] string? appPath = null,
        CancellationToken cancellationToken = default
    )
    {
        var hasJson = !string.IsNullOrWhiteSpace(scenarioJson);
        var hasPath = !string.IsNullOrWhiteSpace(scenarioPath);
        if (hasJson == hasPath)
        {
            return ToolResults.Error(
                "action.failed",
                "Provide exactly one of scenarioJson or scenarioPath."
            );
        }

        try
        {
            var document = hasPath
                ? ScenarioJson.ParseFile(scenarioPath!)
                : ScenarioJson.Parse(scenarioJson!);

            var options = string.IsNullOrWhiteSpace(appPath)
                ? null
                : new ScenarioRunOptions { AppPath = appPath };

            await ScenarioRunner
                .RunAsync(document, options, cancellationToken)
                .ConfigureAwait(false);

            return ToolResults.Ok(
                new JsonObject
                {
                    ["name"] = document.Name,
                    ["operations"] = document.Operations.Count,
                }
            );
        }
        catch (GraftException ex)
        {
            return ToolResults.FromException(ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ToolResults.Error("action.failed", ex.Message);
        }
    }
}
