using Graft.Core.Diagnostics;

namespace Graft.Core.Scenario;

/// <summary>
/// Stable Scenario step <c>action</c> vocabulary (JSON exchange + operation model).
/// </summary>
public static class ScenarioActions
{
    /// <summary>Launch the application under test.</summary>
    public const string Launch = "launch";

    /// <summary>Invoke (click) an element.</summary>
    public const string Invoke = FailureSteps.Invoke;

    /// <summary>Right-click an element.</summary>
    public const string RightClick = FailureSteps.RightClick;

    /// <summary>Double-click an element.</summary>
    public const string DoubleClick = FailureSteps.DoubleClick;

    /// <summary>Hover (move cursor) over an element.</summary>
    public const string Hover = FailureSteps.Hover;

    /// <summary>Drag from one element to another.</summary>
    public const string Drag = FailureSteps.Drag;

    /// <summary>Left-click at clickable point plus DIP offsets.</summary>
    public const string ClickAt = FailureSteps.ClickAt;

    /// <summary>Scroll the mouse wheel over an element.</summary>
    public const string Wheel = FailureSteps.Wheel;

    /// <summary>Replace an element's value.</summary>
    public const string SetValue = FailureSteps.SetValue;

    /// <summary>Toggle an element (e.g. CheckBox).</summary>
    public const string Toggle = FailureSteps.Toggle;

    /// <summary>Type literal text into an element.</summary>
    public const string SendKeys = FailureSteps.SendKeys;

    /// <summary>Press one keyboard chord on an element.</summary>
    public const string PressKeys = FailureSteps.PressKeys;

    /// <summary>Capture the target window screenshot to a path.</summary>
    public const string Screenshot = FailureSteps.Screenshot;

    /// <summary>Scroll an element or list item into view.</summary>
    public const string ScrollIntoView = FailureSteps.ScrollIntoView;

    /// <summary>Select a list/combo item by index.</summary>
    public const string Select = FailureSteps.Select;

    /// <summary>Replace ListBox multi-selection by indexes.</summary>
    public const string SelectMany = FailureSteps.SelectMany;

    /// <summary>Select a menu path (slash-separated AutomationIds).</summary>
    public const string SelectMenu = FailureSteps.SelectMenu;

    /// <summary>Select a tree path (slash-separated AutomationIds).</summary>
    public const string SelectTree = FailureSteps.SelectTree;

    /// <summary>Expand an element.</summary>
    public const string Expand = FailureSteps.Expand;

    /// <summary>Collapse an element.</summary>
    public const string Collapse = FailureSteps.Collapse;

    /// <summary>Expect an element's tree name.</summary>
    public const string ExpectName = FailureSteps.ExpectName;

    /// <summary>Expect an element's selected state.</summary>
    public const string ExpectSelected = FailureSteps.ExpectSelected;

    /// <summary>Expect an element's expanded state.</summary>
    public const string ExpectExpanded = FailureSteps.ExpectExpanded;

    /// <summary>Expect an element's checked state.</summary>
    public const string ExpectChecked = FailureSteps.ExpectChecked;

    /// <summary>Expect an element's enabled state.</summary>
    public const string ExpectEnabled = FailureSteps.ExpectEnabled;

    /// <summary>Expect an element's visible state.</summary>
    public const string ExpectVisible = FailureSteps.ExpectVisible;

    /// <summary>Expect an element's focused state.</summary>
    public const string ExpectFocused = FailureSteps.ExpectFocused;

    /// <summary>Expect an element's name contains a substring.</summary>
    public const string ExpectNameContains = FailureSteps.ExpectNameContains;

    /// <summary>Expect an element's name matches a regex.</summary>
    public const string ExpectNameMatches = FailureSteps.ExpectNameMatches;

    /// <summary>Expect an element's tree value.</summary>
    public const string ExpectValue = FailureSteps.ExpectValue;

    /// <summary>Expect an element's open ToolTip text.</summary>
    public const string ExpectToolTip = FailureSteps.ExpectToolTip;

    /// <summary>Wait until an element is present.</summary>
    public const string WaitFor = FailureSteps.WaitFor;

    /// <summary>Wait until an element is gone or not visible.</summary>
    public const string ExpectGone = FailureSteps.ExpectGone;

    /// <summary>Read a DataGrid cell display text.</summary>
    public const string GetCellText = FailureSteps.GetCellText;

    /// <summary>Set a DataGrid Text cell value.</summary>
    public const string SetCellValue = FailureSteps.SetCellValue;

    /// <summary>Select a DataGrid cell.</summary>
    public const string SelectCell = FailureSteps.SelectCell;

    /// <summary>Select a DataGrid row by column key + cell value.</summary>
    public const string SelectRow = FailureSteps.SelectRow;

    /// <summary>Click a DataGrid column header.</summary>
    public const string ClickColumnHeader = FailureSteps.ClickColumnHeader;

    /// <summary>Add a DataGrid row.</summary>
    public const string AddRow = FailureSteps.AddRow;

    /// <summary>Delete selected DataGrid rows.</summary>
    public const string DeleteSelectedRows = FailureSteps.DeleteSelectedRows;

    /// <summary>Expect a DataGrid cell display text.</summary>
    public const string ExpectCellText = FailureSteps.ExpectCellText;

    /// <summary>Arm the next OpenFile seam with a path (OK).</summary>
    public const string ArmOpenFile = FailureSteps.ArmOpenFile;

    /// <summary>Arm the next OpenFile seam as cancel.</summary>
    public const string ArmOpenFileCancel = FailureSteps.ArmOpenFileCancel;

    /// <summary>Arm the next SaveFile seam with a path (OK).</summary>
    public const string ArmSaveFile = FailureSteps.ArmSaveFile;

    /// <summary>Arm the next SaveFile seam as cancel.</summary>
    public const string ArmSaveFileCancel = FailureSteps.ArmSaveFileCancel;

    /// <summary>Arm the next OpenFolder seam with a path (OK).</summary>
    public const string ArmOpenFolder = FailureSteps.ArmOpenFolder;

    /// <summary>Arm the next OpenFolder seam as cancel.</summary>
    public const string ArmOpenFolderCancel = FailureSteps.ArmOpenFolderCancel;

    /// <summary>Arm the next MessageBox.Show with a MessageBoxResult name.</summary>
    public const string ArmMessageBox = FailureSteps.ArmMessageBox;

    /// <summary>List open windows.</summary>
    public const string ListWindows = FailureSteps.ListWindows;

    /// <summary>Switch the agent target window.</summary>
    public const string SwitchWindow = FailureSteps.SwitchWindow;

    /// <summary>Wait for a window by title and/or automation id.</summary>
    public const string WaitForWindow = FailureSteps.WaitForWindow;

    /// <summary>Wait for a window to close.</summary>
    public const string WaitForWindowClosed = FailureSteps.WaitForWindowClosed;

    /// <summary>Invoke an element that may open a window (modal-safe).</summary>
    public const string InvokeOpeningWindow = FailureSteps.InvokeOpeningWindow;
}
