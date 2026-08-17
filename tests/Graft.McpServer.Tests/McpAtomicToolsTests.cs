using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Graft.McpServer.Tests;

[Collection(McpUiCollection.Name)]
[Trait("Category", "UI")]
public sealed class McpAtomicToolsTests
{
    /// <summary>
    /// MCP atomic tools launch SampleWpfApp, click SampleButton, expect StatusText, then dispose.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Graft.McpServer.dll is built next to the test output
    /// - SampleWpfApp.csproj can build with GraftTest
    ///
    /// Steps:
    /// - graft_launch → graft_invoke(SampleButton) → graft_expect_name(StatusText, Clicked 1) → graft_dispose
    ///
    /// Expected:
    /// - Each CallToolResult IsError is false and ok=true
    /// </remarks>
    [Fact]
    public async Task Launch_Invoke_Expect_Dispose_Succeeds()
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
        Assert.Contains(tools, t => t.Name == "graft_launch");
        Assert.Contains(tools, t => t.Name == "graft_invoke");
        Assert.Contains(tools, t => t.Name == "graft_right_click");
        Assert.Contains(tools, t => t.Name == "graft_set_value");
        Assert.Contains(tools, t => t.Name == "graft_toggle");
        Assert.Contains(tools, t => t.Name == "graft_send_keys");
        Assert.Contains(tools, t => t.Name == "graft_press_keys");
        Assert.Contains(tools, t => t.Name == "graft_screenshot");
        Assert.Contains(tools, t => t.Name == "graft_expect_name");
        Assert.Contains(tools, t => t.Name == "graft_expect_checked");
        Assert.Contains(tools, t => t.Name == "graft_get_cell_text");
        Assert.Contains(tools, t => t.Name == "graft_set_cell_value");
        Assert.Contains(tools, t => t.Name == "graft_expect_cell_text");
        Assert.Contains(tools, t => t.Name == "graft_arm_open_file");
        Assert.Contains(tools, t => t.Name == "graft_arm_open_file_cancel");
        Assert.Contains(tools, t => t.Name == "graft_arm_save_file");
        Assert.Contains(tools, t => t.Name == "graft_arm_save_file_cancel");
        Assert.Contains(tools, t => t.Name == "graft_arm_open_folder");
        Assert.Contains(tools, t => t.Name == "graft_arm_open_folder_cancel");
        Assert.Contains(tools, t => t.Name == "graft_arm_message_box");
        Assert.Contains(tools, t => t.Name == "graft_dispose");

        var appPath = SampleAppLocator.ResolveProjectPath();

        await AssertOkAsync(
            client,
            "graft_launch",
            new Dictionary<string, object?>
            {
                ["appPath"] = appPath,
                ["configuration"] = "GraftTest",
                ["timeoutSeconds"] = 60,
            }
        );

        await AssertOkAsync(
            client,
            "graft_invoke",
            new Dictionary<string, object?> { ["automationId"] = "SampleButton" }
        );

        await AssertOkAsync(
            client,
            "graft_expect_name",
            new Dictionary<string, object?>
            {
                ["automationId"] = "StatusText",
                ["name"] = "Clicked 1",
            }
        );

        await AssertOkAsync(client, "graft_dispose", new Dictionary<string, object?>());
    }

    /// <summary>
    /// graft_invoke without launch returns IsError.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fresh MCP server with no session
    ///
    /// Steps:
    /// - Call graft_invoke
    ///
    /// Expected:
    /// - IsError true; message mentions no open session
    /// </remarks>
    [Fact]
    public async Task Invoke_WithoutLaunch_ReturnsError()
    {
        var serverDll = Path.Combine(AppContext.BaseDirectory, "Graft.McpServer.dll");
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
            "graft_invoke",
            new Dictionary<string, object?> { ["automationId"] = "SampleButton" },
            cancellationToken: CancellationToken.None
        );

        Assert.True(result.IsError);
        var text = GetText(result);
        Assert.Contains("No open session", text, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertOkAsync(
        McpClient client,
        string toolName,
        IReadOnlyDictionary<string, object?> args
    )
    {
        var result = await client.CallToolAsync(
            toolName,
            args,
            cancellationToken: CancellationToken.None
        );
        Assert.False(result.IsError, GetText(result));
        using var doc = JsonDocument.Parse(GetText(result));
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
    }

    private static string GetText(CallToolResult result) =>
        string.Join(string.Empty, result.Content.OfType<TextContentBlock>().Select(b => b.Text));
}
