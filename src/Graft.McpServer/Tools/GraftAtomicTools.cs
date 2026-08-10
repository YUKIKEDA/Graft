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
    /// Scrolls an element or list item into view.
    /// </summary>
    /// <param name="automationId">Element or list automation id.</param>
    /// <param name="index">Optional list item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result including realized identity.</returns>
    [McpServerTool(Name = "graft_scroll_into_view")]
    [Description(
        "scrollIntoView for an element or list item (optional index) in the open session."
    )]
    public Task<CallToolResult> ScrollIntoView(
        [Description("Target element or list automation id.")] string automationId,
        [Description("Optional zero-based list item index.")] int? index = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                var query = session.GetByAutomationId(automationId);
                var identity = index is null
                    ? await query.ScrollIntoViewAsync(cancellationToken).ConfigureAwait(false)
                    : await query
                        .ScrollIntoViewAsync(index.Value, cancellationToken)
                        .ConfigureAwait(false);
                var payload = new JsonObject
                {
                    ["automationId"] = identity.AutomationId,
                    ["listAutomationId"] = automationId,
                };
                if (identity.RuntimeId is { } runtimeId)
                {
                    payload["runtimeId"] = runtimeId;
                }

                if (index is { } itemIndex)
                {
                    payload["index"] = itemIndex;
                }

                return ToolResults.Ok(payload);
            },
            cancellationToken
        );

    /// <summary>
    /// Selects a list/combo item by index.
    /// </summary>
    /// <param name="automationId">List or combo automation id.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select")]
    [Description("Select a single list/combo item by index in the open session.")]
    public Task<CallToolResult> Select(
        [Description("List or combo automation id.")] string automationId,
        [Description("Zero-based item index.")] int index,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SelectAsync(index, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["index"] = index }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Reads DataGrid Text cell display text by row/column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result with text.</returns>
    [McpServerTool(Name = "graft_get_cell_text")]
    [Description("Get DataGrid Text cell display text by row/column in the open session.")]
    public Task<CallToolResult> GetCellText(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Zero-based column index.")] int column,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                var text = await session
                    .GetByAutomationId(automationId)
                    .GetCellTextAsync(row, column, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["row"] = row,
                        ["column"] = column,
                        ["text"] = text,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Sets a DataGrid Text cell value by row/column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="value">Replacement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_set_cell_value")]
    [Description("Set DataGrid Text cell value by row/column in the open session.")]
    public Task<CallToolResult> SetCellValue(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Zero-based column index.")] int column,
        [Description("Replacement text.")] string value,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SetCellValueAsync(row, column, value, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["row"] = row,
                        ["column"] = column,
                        ["value"] = value,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects DataGrid Text cell display text by row/column index.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="text">Expected cell text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_cell_text")]
    [Description("Expect DataGrid Text cell display text by row/column in the open session.")]
    public Task<CallToolResult> ExpectCellText(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Zero-based column index.")] int column,
        [Description("Expected cell text.")] string text,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectCellTextAsync(row, column, text, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["row"] = row,
                        ["column"] = column,
                        ["text"] = text,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expands an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expand")]
    [Description("Expand an element by automationId in the open session.")]
    public Task<CallToolResult> Expand(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpandAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Collapses an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_collapse")]
    [Description("Collapse an element by automationId in the open session.")]
    public Task<CallToolResult> Collapse(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .CollapseAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
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
    /// Expects an element's tree selected state.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="selected">Expected selection state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_selected")]
    [Description("Expect an element's tree selected state in the open session.")]
    public Task<CallToolResult> ExpectSelected(
        [Description("Target automation id.")] string automationId,
        [Description("Expected selected state.")] bool selected,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectSelectedAsync(selected, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["selected"] = selected }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree expanded state.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="expanded">Expected expand state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_expanded")]
    [Description("Expect an element's tree expanded state in the open session.")]
    public Task<CallToolResult> ExpectExpanded(
        [Description("Target automation id.")] string automationId,
        [Description("Expected expanded state.")] bool expanded,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectExpandedAsync(expanded, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["expanded"] = expanded }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree checked state.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="checkedState">Expected checked state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_checked")]
    [Description("Expect an element's tree checked state in the open session.")]
    public Task<CallToolResult> ExpectChecked(
        [Description("Target automation id.")] string automationId,
        [Description("Expected checked state.")] bool checkedState,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectCheckedAsync(checkedState, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["checked"] = checkedState }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Lists open windows in the target process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_list_windows")]
    [Description("List open windows with session-local windowId values.")]
    public Task<CallToolResult> ListWindows(CancellationToken cancellationToken = default) =>
        WithSessionAsync(
            async session =>
            {
                var result = await session
                    .ListWindowsAsync(cancellationToken)
                    .ConfigureAwait(false);
                var windows = new JsonArray();
                foreach (var window in result.Windows)
                {
                    windows.Add(
                        new JsonObject
                        {
                            ["windowId"] = window.WindowId,
                            ["title"] = window.Title,
                            ["automationId"] = window.AutomationId,
                            ["isModal"] = window.IsModal,
                            ["isActive"] = window.IsActive,
                        }
                    );
                }

                return ToolResults.Ok(new JsonObject { ["windows"] = windows });
            },
            cancellationToken
        );

    /// <summary>
    /// Switches the agent target window.
    /// </summary>
    /// <param name="windowId">Session-local window id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_switch_window")]
    [Description("Switch the agent target window by session-local windowId.")]
    public Task<CallToolResult> SwitchWindow(
        [Description("Session-local window id from graft_list_windows.")] int windowId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .SwitchToWindowAsync(windowId, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["windowId"] = windowId });
            },
            cancellationToken
        );

    /// <summary>
    /// Waits for a window by title and/or automation id.
    /// </summary>
    /// <param name="title">Optional exact title.</param>
    /// <param name="automationId">Optional exact automation id.</param>
    /// <param name="switchTo">When true (default), switches to the matched window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_wait_for_window")]
    [Description(
        "Wait for a window by title and/or automationId. Defaults to switching the target to the match."
    )]
    public Task<CallToolResult> WaitForWindow(
        [Description("Exact title (optional).")] string? title = null,
        [Description("Exact automation id (optional).")] string? automationId = null,
        [Description("Switch to match (default true).")] bool switchTo = true,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                var window = await session
                    .WaitForWindowAsync(title, automationId, switchTo, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["windowId"] = window.WindowId,
                        ["title"] = window.Title,
                        ["automationId"] = window.AutomationId,
                        ["isModal"] = window.IsModal,
                        ["isActive"] = window.IsActive,
                        ["switched"] = switchTo,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Invokes an element that may open a window (modal-safe BeginInvoke path).
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="waitForNewWindow">When true (default), wait for a new WPF window and switch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_invoke_opening_window")]
    [Description(
        "Invoke an element that may open a window (BeginInvoke). By default waits for a new WPF window. Set waitForNewWindow=false for Graft OpenFile seam."
    )]
    public Task<CallToolResult> InvokeOpeningWindow(
        [Description("Target automation id.")] string automationId,
        [Description("Wait for new WPF window (default true).")] bool waitForNewWindow = true,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                var window = await session
                    .GetByAutomationId(automationId)
                    .InvokeOpeningWindowAsync(waitForNewWindow, cancellationToken)
                    .ConfigureAwait(false);
                if (window is null)
                {
                    return ToolResults.Ok(
                        new JsonObject
                        {
                            ["automationId"] = automationId,
                            ["waitForNewWindow"] = false,
                        }
                    );
                }

                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["windowId"] = window.WindowId,
                        ["title"] = window.Title,
                        ["windowAutomationId"] = window.AutomationId,
                        ["isModal"] = window.IsModal,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next Graft OpenFile seam with a file path (OK, one-shot).
    /// </summary>
    /// <param name="path">File path to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_open_file")]
    [Description(
        "Arm the next OpenFileDialog.ShowDialog (RunDialog seam) to return a path (one-shot)."
    )]
    public Task<CallToolResult> ArmOpenFile(
        [Description("File path to return.")] string path,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmOpenFileAsync(path, cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["path"] = path });
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next Graft OpenFile seam as cancel (one-shot).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_open_file_cancel")]
    [Description("Arm the next OpenFileDialog.ShowDialog (RunDialog seam) as cancel (one-shot).")]
    public Task<CallToolResult> ArmOpenFileCancel(CancellationToken cancellationToken = default) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmOpenFileCancelAsync(cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["canceled"] = true });
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next Graft SaveFile seam with a file path (OK, one-shot).
    /// </summary>
    /// <param name="path">File path to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_save_file")]
    [Description(
        "Arm the next SaveFileDialog.ShowDialog (RunDialog seam) to return a path (one-shot)."
    )]
    public Task<CallToolResult> ArmSaveFile(
        [Description("File path to return.")] string path,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmSaveFileAsync(path, cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["path"] = path });
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next Graft SaveFile seam as cancel (one-shot).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_save_file_cancel")]
    [Description("Arm the next SaveFileDialog.ShowDialog (RunDialog seam) as cancel (one-shot).")]
    public Task<CallToolResult> ArmSaveFileCancel(CancellationToken cancellationToken = default) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmSaveFileCancelAsync(cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["canceled"] = true });
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
