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
    /// Selects a single item by <c>params.index</c> on a list/combo (<c>params.automationId</c>).
    /// </summary>
    public const string Select = "select";

    /// <summary>
    /// Replaces multi-selection by <c>params.indexes</c> on a ListBox (<c>params.automationId</c>).
    /// Empty <c>indexes</c> clears selection.
    /// </summary>
    public const string SelectMany = "selectMany";

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
