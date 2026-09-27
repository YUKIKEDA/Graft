using System.Text.Json;
using Graft.Core;
using Graft.McpServer.Security;
using Graft.McpServer.Tools;
using Graft.Protocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Graft.McpServer.Tests;

/// <summary>
/// Path confinement for MCP-supplied paths (#87 / #88). Mutates process environment, so it runs in the serial collection.
/// </summary>
[Collection(McpUiCollection.Name)]
public sealed class McpPathPolicyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "graft-mcp-root-" + Guid.NewGuid().ToString("N"));

    public McpPathPolicyTests()
    {
        Directory.CreateDirectory(_root);
        Environment.SetEnvironmentVariable(McpPathPolicy.AllowedRootsEnvironmentVariable, _root);
        Environment.SetEnvironmentVariable(McpPathPolicy.AllowAnyPathEnvironmentVariable, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(McpPathPolicy.AllowedRootsEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(McpPathPolicy.AllowAnyPathEnvironmentVariable, null);
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort temp cleanup.
        }
    }

    /// <summary>
    /// Paths inside the allowed root resolve to full paths.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_MCP_ALLOWED_ROOTS = temp root
    ///
    /// Steps:
    /// - EnsureAllowed for the root itself and a nested file
    ///
    /// Expected:
    /// - Both return their normalized full path
    /// </remarks>
    [Fact]
    public void EnsureAllowed_InsideRoot_ReturnsFullPath()
    {
        var nested = Path.Combine(_root, "sub", "shot.png");

        Assert.Equal(Path.GetFullPath(nested), McpPathPolicy.EnsureAllowed(nested, "path"));
        Assert.Equal(Path.GetFullPath(_root), McpPathPolicy.EnsureAllowed(_root, "path"));
    }

    /// <summary>
    /// Traversal, sibling-prefix, and unrelated absolute paths are rejected.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_MCP_ALLOWED_ROOTS = temp root
    ///
    /// Steps:
    /// - EnsureAllowed for root/../x, a sibling directory sharing the root's name prefix, and an unrelated temp path
    ///
    /// Expected:
    /// - Each throws GraftException action.failed naming the allowed roots
    /// </remarks>
    [Fact]
    public void EnsureAllowed_OutsideRoot_Throws()
    {
        var candidates = new[]
        {
            Path.Combine(_root, "..", "escape.png"),
            _root + "-sibling" + Path.DirectorySeparatorChar + "x.png",
            Path.Combine(Path.GetTempPath(), "elsewhere.exe"),
        };

        foreach (var candidate in candidates)
        {
            var ex = Assert.Throws<GraftException>(() => McpPathPolicy.EnsureAllowed(candidate, "path"));
            Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
            Assert.Contains(McpPathPolicy.AllowedRootsEnvironmentVariable, ex.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// GRAFT_MCP_ALLOW_ANY_PATH=1 disables confinement.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_MCP_ALLOW_ANY_PATH=1
    ///
    /// Steps:
    /// - EnsureAllowed for a path outside the configured root
    ///
    /// Expected:
    /// - Returns the full path without throwing
    /// </remarks>
    [Fact]
    public void EnsureAllowed_AllowAnyPath_DisablesConfinement()
    {
        Environment.SetEnvironmentVariable(McpPathPolicy.AllowAnyPathEnvironmentVariable, "1");
        var outside = Path.Combine(Path.GetTempPath(), "elsewhere.exe");

        Assert.Equal(Path.GetFullPath(outside), McpPathPolicy.EnsureAllowed(outside, "path"));
    }

    /// <summary>
    /// graft_run_scenario rejects a Scenario whose screenshot path escapes the allowed root, before launching anything.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GRAFT_MCP_ALLOWED_ROOTS = temp root
    ///
    /// Steps:
    /// - RunScenario with inline JSON: launch (app inside root) + screenshot to ../../evil.png
    ///
    /// Expected:
    /// - IsError; code action.failed; message mentions screenshot.path
    /// </remarks>
    [Fact]
    public async Task RunScenario_ScreenshotPathOutsideRoot_IsRejected()
    {
        var app = JsonSerializer.Serialize(Path.Combine(_root, "App.exe"));
        var evil = JsonSerializer.Serialize(Path.Combine(_root, "..", "..", "evil.png"));
        var json = $$"""
            {
              "v": 1,
              "name": "escape",
              "steps": [
                { "action": "launch", "appPath": {{app}} },
                { "action": "screenshot", "path": {{evil}} }
              ]
            }
            """;

        var result = await GraftRunScenarioTool.RunScenario(scenarioJson: json);

        Assert.True(result.IsError);
        using var doc = JsonDocument.Parse(GetText(result));
        Assert.Equal(GraftErrorCodes.ActionFailed, doc.RootElement.GetProperty("code").GetString());
        Assert.Contains("screenshot.path", doc.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// graft_launch over stdio refuses an appPath outside the server's allowed roots.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - MCP server started with GRAFT_MCP_ALLOWED_ROOTS = temp root
    ///
    /// Steps:
    /// - CallToolAsync graft_launch with an appPath outside the root
    ///
    /// Expected:
    /// - IsError; code action.failed; message says outside the allowed roots
    /// </remarks>
    [Fact]
    public async Task Launch_AppPathOutsideRoot_IsRejectedOverStdio()
    {
        var serverDll = Path.Combine(AppContext.BaseDirectory, "Graft.McpServer.dll");
        var transport = new StdioClientTransport(
            new StdioClientTransportOptions
            {
                Name = "Graft.McpServer",
                Command = "dotnet",
                Arguments = ["exec", serverDll],
                EnvironmentVariables = new Dictionary<string, string?> { [McpPathPolicy.AllowedRootsEnvironmentVariable] = _root },
            }
        );
        await using var client = await McpClient.CreateAsync(transport);

        var outside = Path.Combine(Path.GetTempPath(), "not-allowed", "App.exe");
        var result = await client.CallToolAsync("graft_launch", new Dictionary<string, object?> { ["appPath"] = outside });

        Assert.True(result.IsError);
        using var doc = JsonDocument.Parse(GetText(result));
        Assert.Equal(GraftErrorCodes.ActionFailed, doc.RootElement.GetProperty("code").GetString());
        Assert.Contains("outside the allowed roots", doc.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    private static string GetText(CallToolResult result) => string.Join(string.Empty, result.Content.OfType<TextContentBlock>().Select(b => b.Text));
}
