using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Graft.McpServer.Tests;

public sealed class McpHostSmokeTests
{
    /// <summary>
    /// Stdio MCP client can list graft_ping from the Graft.McpServer host.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Graft.McpServer.dll is built next to the test output (project reference)
    ///
    /// Steps:
    /// - Start StdioClientTransport with dotnet exec on Graft.McpServer.dll
    /// - ListToolsAsync
    /// - CallToolAsync graft_ping
    ///
    /// Expected:
    /// - Tools include graft_ping
    /// - Result text contains "Graft.McpServer"
    /// </remarks>
    [Fact]
    public async Task ListTools_AndPing_Succeed()
    {
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

        var tools = await client.ListToolsAsync();
        Assert.Contains(tools, t => t.Name == "graft_ping");

        var result = await client.CallToolAsync(
            "graft_ping",
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken.None
        );

        var text = string.Join(
            string.Empty,
            result.Content.OfType<TextContentBlock>().Select(b => b.Text)
        );
        Assert.Contains("Graft.McpServer", text, StringComparison.Ordinal);
        Assert.Contains("\"ok\":true", text, StringComparison.Ordinal);
    }
}
