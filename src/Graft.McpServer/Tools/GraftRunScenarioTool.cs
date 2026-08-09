using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Graft.Core;
using Graft.Core.Diagnostics;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

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
            return ErrorResult(
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

            var ok = new JsonObject
            {
                ["ok"] = true,
                ["name"] = document.Name,
                ["operations"] = document.Operations.Count,
            };
            return TextResult(ok.ToJsonString(JsonOptions), isError: false);
        }
        catch (GraftException ex)
        {
            return ErrorResult(ex.Code, ex.Message, ex.Report);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorResult("action.failed", ex.Message);
        }
    }

    private static CallToolResult ErrorResult(
        string code,
        string message,
        FailureReport? report = null
    )
    {
        var node = new JsonObject
        {
            ["ok"] = false,
            ["code"] = code,
            ["message"] = message,
        };

        if (report is not null)
        {
            node["report"] = JsonNode.Parse(FailureReportJson.Serialize(report));
        }

        return TextResult(node.ToJsonString(JsonOptions), isError: true);
    }

    private static CallToolResult TextResult(string text, bool isError) =>
        new() { IsError = isError, Content = [new TextContentBlock { Text = text }] };
}
