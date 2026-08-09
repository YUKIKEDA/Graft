using System.Text.Json;
using System.Text.Json.Nodes;
using Graft.Core;
using Graft.Core.Diagnostics;
using ModelContextProtocol.Protocol;

namespace Graft.McpServer.Tools;

/// <summary>
/// Shared JSON <see cref="CallToolResult"/> helpers for Graft MCP tools.
/// </summary>
internal static class ToolResults
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    public static CallToolResult Ok(JsonObject payload)
    {
        payload["ok"] = true;
        return Text(payload.ToJsonString(JsonOptions), isError: false);
    }

    public static CallToolResult Error(string code, string message, FailureReport? report = null)
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

        return Text(node.ToJsonString(JsonOptions), isError: true);
    }

    public static CallToolResult FromException(GraftException ex) =>
        Error(ex.Code, ex.Message, ex.Report);

    private static CallToolResult Text(string text, bool isError) =>
        new() { IsError = isError, Content = [new TextContentBlock { Text = text }] };
}
