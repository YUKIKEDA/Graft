using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Graft.McpServer.Tools;

/// <summary>
/// Minimal health tool to verify the MCP host is reachable.
/// </summary>
[McpServerToolType]
public static class GraftPingTool
{
    /// <summary>
    /// Returns a fixed pong payload (no UI session required).
    /// </summary>
    /// <returns>JSON text confirming the Graft MCP server is alive.</returns>
    [McpServerTool(Name = "graft_ping")]
    [Description("Health check for the Graft MCP server. Returns a pong payload.")]
    public static string Ping() => """{"ok":true,"server":"Graft.McpServer"}""";
}
