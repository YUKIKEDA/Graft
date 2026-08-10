namespace Graft.Protocol;

/// <summary>
/// Wire <c>method</c> names used in request envelopes (camelCase).
/// </summary>
public static class ProtocolMethods
{
    /// <summary>
    /// Establishes a session: protocol version must match and <c>params.token</c> must equal
    /// <c>GRAFT_CONNECT_TOKEN</c>.
    /// </summary>
    public const string Handshake = "handshake";

    /// <summary>
    /// Returns the UI visual tree for the current target window.
    /// </summary>
    public const string GetTree = "getTree";

    /// <summary>
    /// Lists open windows with session-local <c>windowId</c> values.
    /// </summary>
    public const string ListWindows = "listWindows";

    /// <summary>
    /// Switches the agent target window via <c>params.windowId</c>.
    /// </summary>
    public const string SwitchWindow = "switchWindow";

    /// <summary>
    /// Begins an invoke that may open a window (non-blocking on the UI thread).
    /// </summary>
    public const string InvokeOpeningWindow = "invokeOpeningWindow";

    /// <summary>
    /// Captures a window screenshot: JSON meta result followed by a raw PNG frame.
    /// </summary>
    public const string Screenshot = "screenshot";

    /// <summary>
    /// Invokes an element (e.g. button click) selected by <c>params.automationId</c>.
    /// </summary>
    public const string Invoke = "invoke";

    /// <summary>
    /// Right-clicks an element selected by <c>params.automationId</c> (SendInput).
    /// </summary>
    public const string RightClick = "rightClick";

    /// <summary>
    /// Double-clicks an element selected by <c>params.automationId</c> (SendInput).
    /// </summary>
    public const string DoubleClick = "doubleClick";

    /// <summary>
    /// Moves the cursor over an element selected by <c>params.automationId</c> (SendInput).
    /// </summary>
    public const string Hover = "hover";

    /// <summary>
    /// Drags from <c>params.automationId</c> to <c>params.toAutomationId</c> (SendInput).
    /// </summary>
    public const string Drag = "drag";

    /// <summary>
    /// Left-clicks at clickable point + DIP offsets via <c>params.automationId</c>, <c>offsetX</c>, <c>offsetY</c>.
    /// </summary>
    public const string ClickAt = "clickAt";

    /// <summary>
    /// Scrolls the mouse wheel over <c>params.automationId</c> by <c>params.delta</c> (SendInput).
    /// </summary>
    public const string Wheel = "wheel";

    /// <summary>
    /// Replaces an element's value (e.g. TextBox) via <c>params.automationId</c> and <c>params.value</c>.
    /// </summary>
    public const string SetValue = "setValue";

    /// <summary>
    /// Toggles an element (e.g. CheckBox) selected by <c>params.automationId</c>.
    /// </summary>
    public const string Toggle = "toggle";

    /// <summary>
    /// Types literal text into a focused element via <c>params.automationId</c> and <c>params.text</c>.
    /// </summary>
    public const string SendKeys = "sendKeys";

    /// <summary>
    /// Presses one keyboard chord on a focused element via <c>params.automationId</c> and <c>params.keys</c>.
    /// </summary>
    public const string PressKeys = "pressKeys";

    /// <summary>
    /// Scrolls an element into view. Optional <c>params.index</c> targets a list item.
    /// </summary>
    public const string ScrollIntoView = "scrollIntoView";

    /// <summary>
    /// Selects a single item on a list/combo/tab (<c>params.automationId</c>) by exactly one of
    /// <c>params.index</c> or <c>params.key</c> (display / Automation Name; ordinal).
    /// </summary>
    public const string Select = "select";

    /// <summary>
    /// Replaces multi-selection by <c>params.indexes</c> on a ListBox (<c>params.automationId</c>).
    /// Empty <c>indexes</c> clears selection.
    /// </summary>
    public const string SelectMany = "selectMany";

    /// <summary>
    /// Selects a menu path under <c>params.automationId</c> via slash-separated <c>params.path</c>
    /// (AutomationId segments; Menu or open ContextMenu root).
    /// </summary>
    public const string SelectMenu = "selectMenu";

    /// <summary>
    /// Selects a TreeView path under <c>params.automationId</c> via slash-separated <c>params.path</c>
    /// (AutomationId segments; expands intermediates, selects the leaf).
    /// </summary>
    public const string SelectTree = "selectTree";

    /// <summary>
    /// Expands an element (e.g. TreeViewItem / Expander) by <c>params.automationId</c>.
    /// </summary>
    public const string Expand = "expand";

    /// <summary>
    /// Collapses an element (e.g. TreeViewItem / Expander) by <c>params.automationId</c>.
    /// </summary>
    public const string Collapse = "collapse";

    /// <summary>
    /// Returns DataGrid cell display text via <c>params.automationId</c>, <c>row</c>, <c>column</c>.
    /// </summary>
    public const string GetCellText = "getCellText";

    /// <summary>
    /// Sets a DataGrid Text cell via <c>params.automationId</c>, <c>row</c>, <c>column</c>, <c>value</c>.
    /// </summary>
    public const string SetCellValue = "setCellValue";

    /// <summary>
    /// Selects a single DataGrid cell via <c>params.automationId</c>, <c>row</c>, and
    /// exactly one of <c>column</c> / <c>columnKey</c>.
    /// </summary>
    public const string SelectCell = "selectCell";

    /// <summary>
    /// Selects a DataGrid row by <c>params.columnKey</c> + <c>params.value</c> cell match.
    /// </summary>
    public const string SelectRow = "selectRow";

    /// <summary>
    /// Clicks a DataGrid column header via <c>params.automationId</c> + <c>params.columnKey</c>.
    /// </summary>
    public const string ClickColumnHeader = "clickColumnHeader";

    /// <summary>
    /// Adds a new DataGrid row via <c>params.automationId</c>.
    /// </summary>
    public const string AddRow = "addRow";

    /// <summary>
    /// Deletes selected DataGrid rows via <c>params.automationId</c>.
    /// </summary>
    public const string DeleteSelectedRows = "deleteSelectedRows";

    /// <summary>
    /// Arms the next Graft OpenFile seam response with <c>params.path</c> (OK).
    /// </summary>
    public const string ArmOpenFile = "armOpenFile";

    /// <summary>
    /// Arms the next Graft OpenFile seam response as cancel.
    /// </summary>
    public const string ArmOpenFileCancel = "armOpenFileCancel";

    /// <summary>
    /// Arms the next Graft SaveFile seam response with <c>params.path</c> (OK).
    /// </summary>
    public const string ArmSaveFile = "armSaveFile";

    /// <summary>
    /// Arms the next Graft SaveFile seam response as cancel.
    /// </summary>
    public const string ArmSaveFileCancel = "armSaveFileCancel";

    /// <summary>
    /// Arms the next Graft OpenFolder seam response with <c>params.path</c> (OK).
    /// </summary>
    public const string ArmOpenFolder = "armOpenFolder";

    /// <summary>
    /// Arms the next Graft OpenFolder seam response as cancel.
    /// </summary>
    public const string ArmOpenFolderCancel = "armOpenFolderCancel";

    /// <summary>
    /// Arms the next Graft MessageBox seam with <c>params.result</c> (MessageBoxResult name).
    /// </summary>
    public const string ArmMessageBox = "armMessageBox";
}
