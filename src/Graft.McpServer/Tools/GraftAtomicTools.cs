using System.ComponentModel;
using System.Text.Json.Nodes;
using Graft.Core;
using Graft.McpServer.Session;
using Graft.Protocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Graft.McpServer.Tools;

/// <summary>
/// Session-scoped atomic MCP tools wrapping Fluent Core APIs.
/// </summary>
[McpServerToolType]
public sealed class GraftAtomicTools
{
    private readonly GraftSessionHub _hub;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraftAtomicTools"/> class.
    /// </summary>
    /// <param name="hub">Process-wide session hub.</param>
    public GraftAtomicTools(GraftSessionHub hub)
    {
        _hub = hub;
    }

    /// <summary>
    /// Launches an instrumented app and opens the MCP session.
    /// </summary>
    /// <param name="appPath">Absolute path to exe or csproj.</param>
    /// <param name="configuration">MSBuild configuration when <paramref name="appPath"/> is a csproj.</param>
    /// <param name="timeoutSeconds">Launch + handshake budget in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_launch")]
    [Description(
        "Launch an instrumented app and open a Graft session. Fails if a session is already open."
    )]
    public Task<CallToolResult> Launch(
        [Description("Absolute path to the app exe or csproj.")] string appPath,
        [Description("MSBuild configuration (default GraftTest).")] string? configuration = null,
        [Description("Launch timeout in seconds (default 30).")] double? timeoutSeconds = null,
        CancellationToken cancellationToken = default
    ) =>
        _hub.RunAsync(
            async session =>
            {
                if (session is not null)
                {
                    return ToolResults.Error(
                        GraftErrorCodes.ActionFailed,
                        "A session is already open. Call graft_dispose first."
                    );
                }

                if (string.IsNullOrWhiteSpace(appPath))
                {
                    return ToolResults.Error(
                        GraftErrorCodes.ActionFailed,
                        "appPath must be non-empty."
                    );
                }

                try
                {
                    var options = new LaunchOptions
                    {
                        AppPath = appPath,
                        Configuration = string.IsNullOrWhiteSpace(configuration)
                            ? "GraftTest"
                            : configuration!,
                        Timeout = timeoutSeconds is > 0
                            ? TimeSpan.FromSeconds(timeoutSeconds.Value)
                            : LaunchOptions.DefaultTimeout,
                    };

                    var launched = await Application
                        .LaunchAsync(options, cancellationToken)
                        .ConfigureAwait(false);
                    _hub.SetSession(launched);
                    return ToolResults.Ok(
                        new JsonObject
                        {
                            ["processId"] = launched.ProcessId,
                            ["appPath"] = Path.GetFullPath(appPath),
                        }
                    );
                }
                catch (GraftException ex)
                {
                    return ToolResults.FromException(ex);
                }
            },
            cancellationToken
        );

    /// <summary>
    /// Invokes an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_invoke")]
    [Description("Invoke (click) an element by automationId in the open session.")]
    public Task<CallToolResult> Invoke(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .InvokeAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Sets an element's value by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_set_value")]
    [Description("setValue on an element by automationId in the open session.")]
    public Task<CallToolResult> SetValue(
        [Description("Target automation id.")] string automationId,
        [Description("Replacement text.")] string value,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SetValueAsync(value, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["value"] = value }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Toggles an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_toggle")]
    [Description("Toggle an element by automationId in the open session.")]
    public Task<CallToolResult> Toggle(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ToggleAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Types literal text into an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="text">Literal text (no chord DSL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_send_keys")]
    [Description("sendKeys (literal text) to an element by automationId in the open session.")]
    public Task<CallToolResult> SendKeys(
        [Description("Target automation id.")] string automationId,
        [Description("Literal text to type.")] string text,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SendKeysAsync(text, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["text"] = text }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree name.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="name">Expected name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_name")]
    [Description("Expect an element's tree name in the open session.")]
    public Task<CallToolResult> ExpectName(
        [Description("Target automation id.")] string automationId,
        [Description("Expected tree name.")] string name,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectNameAsync(name, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["name"] = name }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Disposes the open Graft session (pipe + child process).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_dispose")]
    [Description("Dispose the open Graft session (closes pipe and kills the app process).")]
    public Task<CallToolResult> DisposeSession(CancellationToken cancellationToken = default) =>
        _hub.RunAsync(
            async session =>
            {
                if (session is null)
                {
                    return ToolResults.Error(
                        GraftErrorCodes.ActionFailed,
                        "No open session to dispose."
                    );
                }

                await session.DisposeAsync().ConfigureAwait(false);
                _hub.SetSession(null);
                return ToolResults.Ok(new JsonObject { ["disposed"] = true });
            },
            cancellationToken
        );

    private Task<CallToolResult> WithSessionAsync(
        Func<GraftSession, Task<CallToolResult>> action,
        CancellationToken cancellationToken
    ) =>
        _hub.RunAsync(
            async session =>
            {
                if (session is null)
                {
                    return ToolResults.Error(
                        GraftErrorCodes.ActionFailed,
                        "No open session. Call graft_launch first."
                    );
                }

                try
                {
                    return await action(session).ConfigureAwait(false);
                }
                catch (GraftException ex)
                {
                    return ToolResults.FromException(ex);
                }
            },
            cancellationToken
        );
}
