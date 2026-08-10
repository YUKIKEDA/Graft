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
public sealed partial class GraftAtomicTools
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
    public partial Task<CallToolResult> Launch(
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
    public partial Task<CallToolResult> Invoke(
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
    /// Right-clicks an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_right_click")]
    [Description("rightClick an element by automationId in the open session.")]
    public partial Task<CallToolResult> RightClick(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .RightClickAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Double-clicks an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_double_click")]
    [Description("doubleClick an element by automationId in the open session.")]
    public partial Task<CallToolResult> DoubleClick(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .DoubleClickAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Hovers over an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_hover")]
    [Description("hover over an element by automationId in the open session.")]
    public partial Task<CallToolResult> Hover(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .HoverAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Drags from one element to another by automation id.
    /// </summary>
    /// <param name="automationId">Source automation id.</param>
    /// <param name="toAutomationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_drag")]
    [Description("drag from automationId to toAutomationId in the open session.")]
    public partial Task<CallToolResult> Drag(
        [Description("Source automation id.")] string automationId,
        [Description("Target automation id.")] string toAutomationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .DragAsync(toAutomationId, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["toAutomationId"] = toAutomationId,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Left-clicks at clickable point plus DIP offsets.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="offsetX">Horizontal DIP offset from clickable point.</param>
    /// <param name="offsetY">Vertical DIP offset from clickable point.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_click_at")]
    [Description("clickAt an element with DIP offsets in the open session.")]
    public partial Task<CallToolResult> ClickAt(
        [Description("Target automation id.")] string automationId,
        [Description("Horizontal DIP offset from clickable point.")] double offsetX,
        [Description("Vertical DIP offset from clickable point.")] double offsetY,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ClickAtAsync(offsetX, offsetY, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["offsetX"] = offsetX,
                        ["offsetY"] = offsetY,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Scrolls the mouse wheel over an element.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="delta">Wheel delta (typically multiples of 120).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_wheel")]
    [Description("wheel over an element by automationId in the open session.")]
    public partial Task<CallToolResult> Wheel(
        [Description("Target automation id.")] string automationId,
        [Description("Wheel delta (typically multiples of 120).")] int delta,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .WheelAsync(delta, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["delta"] = delta }
                );
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
    public partial Task<CallToolResult> SetValue(
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
    public partial Task<CallToolResult> Toggle(
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
    public partial Task<CallToolResult> SendKeys(
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
    /// Presses one keyboard chord on an element by automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="keys">Chord DSL (e.g. <c>Control+A</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_press_keys")]
    [Description(
        "pressKeys: one keyboard chord (e.g. Control+A, Delete) on an element by automationId."
    )]
    public partial Task<CallToolResult> PressKeys(
        [Description("Target automation id.")] string automationId,
        [Description("Chord DSL (one chord per call).")] string keys,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .PressAsync(keys, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["keys"] = keys }
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
    public partial Task<CallToolResult> ScrollIntoView(
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
    public partial Task<CallToolResult> Select(
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
    /// Selects a list/combo/tab item by display name key.
    /// </summary>
    /// <param name="automationId">List or combo automation id.</param>
    /// <param name="key">Item name key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select_by_key")]
    [Description("Select a list/combo/tab item by name key in the open session.")]
    public partial Task<CallToolResult> SelectByKey(
        [Description("List or combo automation id.")] string automationId,
        [Description("Item display / automation name.")] string key,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SelectAsync(key, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["key"] = key }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Selects a TreeView path under a TreeView root.
    /// </summary>
    /// <param name="automationId">TreeView automation id.</param>
    /// <param name="path">Slash-separated AutomationId path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select_tree")]
    [Description("selectTree under TreeView automationId via slash-separated AutomationId path.")]
    public partial Task<CallToolResult> SelectTree(
        [Description("TreeView automation id.")] string automationId,
        [Description("Slash-separated AutomationId path (root not included).")] string path,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SelectTreeAsync(path, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["path"] = path }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Replaces ListBox or DataGrid multi-selection by indexes (empty clears).
    /// </summary>
    /// <param name="automationId">ListBox or DataGrid automation id.</param>
    /// <param name="indexes">Zero-based item/row indexes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select_many")]
    [Description(
        "Replace ListBox or DataGrid multi-selection by indexes in the open session (empty clears)."
    )]
    public partial Task<CallToolResult> SelectMany(
        [Description("ListBox or DataGrid automation id.")] string automationId,
        [Description("Zero-based item/row indexes (empty clears selection).")] int[] indexes,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                ArgumentNullException.ThrowIfNull(indexes);
                await session
                    .GetByAutomationId(automationId)
                    .SelectManyAsync(indexes, cancellationToken)
                    .ConfigureAwait(false);
                var indexesJson = new JsonArray();
                foreach (var index in indexes)
                {
                    indexesJson.Add(index);
                }

                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["indexes"] = indexesJson }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Selects a menu path under a Menu or open ContextMenu by automation id.
    /// </summary>
    /// <param name="automationId">Menu or open ContextMenu automation id.</param>
    /// <param name="path">Slash-separated AutomationId segments (root not included).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select_menu")]
    [Description(
        "selectMenu under Menu/ContextMenu automationId via slash-separated AutomationId path."
    )]
    public partial Task<CallToolResult> SelectMenu(
        [Description("Menu or open ContextMenu automation id.")] string automationId,
        [Description("Slash-separated AutomationId path (root not included).")] string path,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SelectMenuAsync(path, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["path"] = path }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Reads DataGrid cell display text by row and column index or Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index (xor columnKey).</param>
    /// <param name="columnKey">Column Header string (xor column).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result with text.</returns>
    [McpServerTool(Name = "graft_get_cell_text")]
    [Description(
        "Get DataGrid cell text by row and column index or columnKey (Header) in the open session."
    )]
    public partial Task<CallToolResult> GetCellText(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Column index (xor columnKey).")] int? column = null,
        [Description("Column Header (xor column).")] string? columnKey = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                EnsureColumnXor(column, columnKey);
                var text = columnKey is null
                    ? await session
                        .GetByAutomationId(automationId)
                        .GetCellTextAsync(row, column!.Value, cancellationToken)
                        .ConfigureAwait(false)
                    : await session
                        .GetByAutomationId(automationId)
                        .GetCellTextAsync(row, columnKey, cancellationToken)
                        .ConfigureAwait(false);
                var payload = new JsonObject
                {
                    ["automationId"] = automationId,
                    ["row"] = row,
                    ["text"] = text,
                };
                if (columnKey is null)
                {
                    payload["column"] = column;
                }
                else
                {
                    payload["columnKey"] = columnKey;
                }

                return ToolResults.Ok(payload);
            },
            cancellationToken
        );

    /// <summary>
    /// Sets a DataGrid cell value by row and column index or Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="value">Replacement text (CheckBox: True/False).</param>
    /// <param name="column">Zero-based column index (xor columnKey).</param>
    /// <param name="columnKey">Column Header string (xor column).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_set_cell_value")]
    [Description(
        "Set DataGrid cell value by row and column index or columnKey (Header) in the open session."
    )]
    public partial Task<CallToolResult> SetCellValue(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Replacement text (CheckBox: True/False).")] string value,
        [Description("Column index (xor columnKey).")] int? column = null,
        [Description("Column Header (xor column).")] string? columnKey = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                EnsureColumnXor(column, columnKey);
                if (columnKey is null)
                {
                    await session
                        .GetByAutomationId(automationId)
                        .SetCellValueAsync(row, column!.Value, value, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await session
                        .GetByAutomationId(automationId)
                        .SetCellValueAsync(row, columnKey, value, cancellationToken)
                        .ConfigureAwait(false);
                }

                var payload = new JsonObject
                {
                    ["automationId"] = automationId,
                    ["row"] = row,
                    ["value"] = value,
                };
                if (columnKey is null)
                {
                    payload["column"] = column;
                }
                else
                {
                    payload["columnKey"] = columnKey;
                }

                return ToolResults.Ok(payload);
            },
            cancellationToken
        );

    /// <summary>
    /// Selects a DataGrid cell by row and column index or Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index (xor columnKey).</param>
    /// <param name="columnKey">Column Header string (xor column).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select_cell")]
    [Description("selectCell on a DataGrid by row and column/columnKey in the open session.")]
    public Task<CallToolResult> SelectCell(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Column index (xor columnKey).")] int? column = null,
        [Description("Column Header (xor column).")] string? columnKey = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                EnsureColumnXor(column, columnKey);
                if (columnKey is null)
                {
                    await session
                        .GetByAutomationId(automationId)
                        .SelectCellAsync(row, column!.Value, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await session
                        .GetByAutomationId(automationId)
                        .SelectCellAsync(row, columnKey, cancellationToken)
                        .ConfigureAwait(false);
                }

                var payload = new JsonObject { ["automationId"] = automationId, ["row"] = row };
                if (columnKey is null)
                {
                    payload["column"] = column;
                }
                else
                {
                    payload["columnKey"] = columnKey;
                }

                return ToolResults.Ok(payload);
            },
            cancellationToken
        );

    /// <summary>
    /// Selects a DataGrid row by column Header key and cell value.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Exact cell display text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_select_row")]
    [Description("selectRow on a DataGrid by columnKey + value in the open session.")]
    public Task<CallToolResult> SelectRow(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Column Header.")] string columnKey,
        [Description("Exact cell display text.")] string value,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .SelectRowAsync(columnKey, value, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["automationId"] = automationId,
                        ["columnKey"] = columnKey,
                        ["value"] = value,
                    }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Clicks a DataGrid column header (sort UI).
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_click_column_header")]
    [Description("clickColumnHeader on a DataGrid in the open session.")]
    public Task<CallToolResult> ClickColumnHeader(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Column Header.")] string columnKey,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ClickColumnHeaderAsync(columnKey, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["columnKey"] = columnKey }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Adds a DataGrid row.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_add_row")]
    [Description("addRow on a DataGrid in the open session.")]
    public Task<CallToolResult> AddRow(
        [Description("DataGrid automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .AddRowAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Deletes selected DataGrid rows.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_delete_selected_rows")]
    [Description("deleteSelectedRows on a DataGrid in the open session.")]
    public Task<CallToolResult> DeleteSelectedRows(
        [Description("DataGrid automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .DeleteSelectedRowsAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Expects DataGrid cell display text by row and column index or Header key.
    /// </summary>
    /// <param name="automationId">DataGrid automation id.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="text">Expected cell text.</param>
    /// <param name="column">Zero-based column index (xor columnKey).</param>
    /// <param name="columnKey">Column Header string (xor column).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_cell_text")]
    [Description(
        "Expect DataGrid cell text by row and column index or columnKey (Header) in the open session."
    )]
    public partial Task<CallToolResult> ExpectCellText(
        [Description("DataGrid automation id.")] string automationId,
        [Description("Zero-based row index.")] int row,
        [Description("Expected cell text.")] string text,
        [Description("Column index (xor columnKey).")] int? column = null,
        [Description("Column Header (xor column).")] string? columnKey = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                EnsureColumnXor(column, columnKey);
                if (columnKey is null)
                {
                    await session
                        .GetByAutomationId(automationId)
                        .ExpectCellTextAsync(row, column!.Value, text, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await session
                        .GetByAutomationId(automationId)
                        .ExpectCellTextAsync(row, columnKey, text, cancellationToken)
                        .ConfigureAwait(false);
                }

                var payload = new JsonObject
                {
                    ["automationId"] = automationId,
                    ["row"] = row,
                    ["text"] = text,
                };
                if (columnKey is null)
                {
                    payload["column"] = column;
                }
                else
                {
                    payload["columnKey"] = columnKey;
                }

                return ToolResults.Ok(payload);
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
    public partial Task<CallToolResult> Expand(
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
    public partial Task<CallToolResult> Collapse(
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
    public partial Task<CallToolResult> ExpectName(
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
    public partial Task<CallToolResult> ExpectSelected(
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
    public partial Task<CallToolResult> ExpectExpanded(
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
    public partial Task<CallToolResult> ExpectChecked(
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
    /// Expects an element's tree enabled state.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="enabled">Expected enabled state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_enabled")]
    [Description("Expect an element's tree enabled state in the open session.")]
    public partial Task<CallToolResult> ExpectEnabled(
        [Description("Target automation id.")] string automationId,
        [Description("Expected enabled state.")] bool enabled,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectEnabledAsync(enabled, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["enabled"] = enabled }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree visible state.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="visible">Expected visible state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_visible")]
    [Description("Expect an element's tree visible state in the open session.")]
    public partial Task<CallToolResult> ExpectVisible(
        [Description("Target automation id.")] string automationId,
        [Description("Expected visible state.")] bool visible,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectVisibleAsync(visible, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["visible"] = visible }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree name contains a substring.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="substring">Expected ordinal substring.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_name_contains")]
    [Description("Expect an element's tree name contains a substring in the open session.")]
    public partial Task<CallToolResult> ExpectNameContains(
        [Description("Target automation id.")] string automationId,
        [Description("Expected ordinal substring.")] string substring,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectNameContainsAsync(substring, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["substring"] = substring }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree name matches a regex pattern.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="pattern">.NET regular expression pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_name_matches")]
    [Description("Expect an element's tree name matches a regex in the open session.")]
    public partial Task<CallToolResult> ExpectNameMatches(
        [Description("Target automation id.")] string automationId,
        [Description(".NET regular expression pattern.")] string pattern,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectNameMatchesAsync(pattern, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["pattern"] = pattern }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Expects an element's tree value.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="value">Expected tree value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_value")]
    [Description("Expect an element's tree value in the open session.")]
    public partial Task<CallToolResult> ExpectValue(
        [Description("Target automation id.")] string automationId,
        [Description("Expected tree value.")] string value,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectValueAsync(value, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject { ["automationId"] = automationId, ["value"] = value }
                );
            },
            cancellationToken
        );

    /// <summary>
    /// Waits until an element is present in the visual tree.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_wait_for")]
    [Description("Wait until an element is present in the open session.")]
    public partial Task<CallToolResult> WaitFor(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .WaitForAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Waits until an element is not found or not visible.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_expect_gone")]
    [Description("Wait until an element is gone or not visible in the open session.")]
    public partial Task<CallToolResult> ExpectGone(
        [Description("Target automation id.")] string automationId,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .GetByAutomationId(automationId)
                    .ExpectGoneAsync(cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["automationId"] = automationId });
            },
            cancellationToken
        );

    /// <summary>
    /// Captures a PNG screenshot of the current target window and writes it to a path.
    /// </summary>
    /// <param name="path">Destination PNG path (optional; temp file when omitted).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result with meta and path.</returns>
    [McpServerTool(Name = "graft_screenshot")]
    [Description(
        "Capture the current target window as PNG. Optional path; when omitted writes a temp file."
    )]
    public partial Task<CallToolResult> Screenshot(
        [Description("Destination PNG path (optional; temp when omitted).")] string? path = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                var shot = await session.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
                var dest = string.IsNullOrWhiteSpace(path)
                    ? Path.Combine(Path.GetTempPath(), $"graft-mcp-{Guid.NewGuid():N}.png")
                    : path;
                await shot.SaveAsync(dest, cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["format"] = shot.Format,
                        ["width"] = shot.Width,
                        ["height"] = shot.Height,
                        ["byteLength"] = shot.PngBytes.Length,
                        ["path"] = Path.GetFullPath(dest),
                    }
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
    public partial Task<CallToolResult> ListWindows(
        CancellationToken cancellationToken = default
    ) =>
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
    public partial Task<CallToolResult> SwitchWindow(
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
    public partial Task<CallToolResult> WaitForWindow(
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
    /// Waits until a window by title and/or automation id is closed.
    /// </summary>
    /// <param name="title">Optional exact title.</param>
    /// <param name="automationId">Optional exact automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_wait_for_window_closed")]
    [Description("Wait until a window by title and/or automationId is closed.")]
    public partial Task<CallToolResult> WaitForWindowClosed(
        [Description("Exact title (optional).")] string? title = null,
        [Description("Exact automation id (optional).")] string? automationId = null,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session
                    .WaitForWindowClosedAsync(title, automationId, cancellationToken)
                    .ConfigureAwait(false);
                return ToolResults.Ok(
                    new JsonObject
                    {
                        ["title"] = title,
                        ["automationId"] = automationId,
                        ["closed"] = true,
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
    public partial Task<CallToolResult> InvokeOpeningWindow(
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
    public partial Task<CallToolResult> ArmOpenFile(
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
    public partial Task<CallToolResult> ArmOpenFileCancel(
        CancellationToken cancellationToken = default
    ) =>
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
    public partial Task<CallToolResult> ArmSaveFile(
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
    public partial Task<CallToolResult> ArmSaveFileCancel(
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmSaveFileCancelAsync(cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["canceled"] = true });
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next Graft OpenFolder seam with a folder path (OK, one-shot).
    /// </summary>
    /// <param name="path">Folder path to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_open_folder")]
    [Description(
        "Arm the next OpenFolderDialog.ShowDialog (RunDialog seam) to return a folder path (one-shot)."
    )]
    public partial Task<CallToolResult> ArmOpenFolder(
        [Description("Folder path to return.")] string path,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmOpenFolderAsync(path, cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["path"] = path });
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next Graft OpenFolder seam as cancel (one-shot).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_open_folder_cancel")]
    [Description("Arm the next OpenFolderDialog.ShowDialog (RunDialog seam) as cancel (one-shot).")]
    public partial Task<CallToolResult> ArmOpenFolderCancel(
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmOpenFolderCancelAsync(cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["canceled"] = true });
            },
            cancellationToken
        );

    /// <summary>
    /// Arms the next MessageBox.Show with a MessageBoxResult (one-shot).
    /// </summary>
    /// <param name="result">MessageBoxResult name: None, OK, Cancel, Yes, or No.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON tool result.</returns>
    [McpServerTool(Name = "graft_arm_message_box")]
    [Description(
        "Arm the next MessageBox.Show to return a MessageBoxResult (None/OK/Cancel/Yes/No, one-shot)."
    )]
    public partial Task<CallToolResult> ArmMessageBox(
        [Description("MessageBoxResult name: None, OK, Cancel, Yes, or No.")] string result,
        CancellationToken cancellationToken = default
    ) =>
        WithSessionAsync(
            async session =>
            {
                await session.ArmMessageBoxAsync(result, cancellationToken).ConfigureAwait(false);
                return ToolResults.Ok(new JsonObject { ["result"] = result });
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
    public partial Task<CallToolResult> DisposeSession(
        CancellationToken cancellationToken = default
    ) =>
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

    private static void EnsureColumnXor(int? column, string? columnKey)
    {
        var hasColumn = column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(columnKey);
        if (hasColumn == hasKey)
        {
            throw new GraftException(
                GraftErrorCodes.SelectorInvalid,
                "Exactly one of column or columnKey is required."
            );
        }
    }

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
