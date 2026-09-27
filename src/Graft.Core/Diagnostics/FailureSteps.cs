using Graft.Protocol;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Stable <see cref="FailureReport.Step"/> vocabulary for Fluent / Scenario failures.
/// </summary>
/// <remarks>
/// A constant that names a wire method aliases <see cref="ProtocolMethods"/>.
/// Steps that exist only in Core, such as <c>expectName</c>, <c>waitFor</c>, and <c>softAssert</c>, stay as literals here.
/// </remarks>
public static class FailureSteps
{
    /// <summary>Wait until present / actionable timed out or failed.</summary>
    public const string Wait = "wait";

    /// <summary>Invoke (click) action failed.</summary>
    public const string Invoke = ProtocolMethods.Invoke;

    /// <summary>rightClick action failed.</summary>
    public const string RightClick = ProtocolMethods.RightClick;

    /// <summary>doubleClick action failed.</summary>
    public const string DoubleClick = ProtocolMethods.DoubleClick;

    /// <summary>hover action failed.</summary>
    public const string Hover = ProtocolMethods.Hover;

    /// <summary>drag action failed.</summary>
    public const string Drag = ProtocolMethods.Drag;

    /// <summary>clickAt action failed.</summary>
    public const string ClickAt = ProtocolMethods.ClickAt;

    /// <summary>wheel action failed.</summary>
    public const string Wheel = ProtocolMethods.Wheel;

    /// <summary>setValue action failed.</summary>
    public const string SetValue = ProtocolMethods.SetValue;

    /// <summary>toggle action failed.</summary>
    public const string Toggle = ProtocolMethods.Toggle;

    /// <summary>sendKeys action failed.</summary>
    public const string SendKeys = ProtocolMethods.SendKeys;

    /// <summary>typeHuman action failed.</summary>
    public const string TypeHuman = ProtocolMethods.TypeHuman;

    /// <summary>pressKeys action failed.</summary>
    public const string PressKeys = ProtocolMethods.PressKeys;

    /// <summary>screenshot action failed.</summary>
    public const string Screenshot = ProtocolMethods.Screenshot;

    /// <summary>scrollIntoView action failed.</summary>
    public const string ScrollIntoView = ProtocolMethods.ScrollIntoView;

    /// <summary>select action failed.</summary>
    public const string Select = ProtocolMethods.Select;

    /// <summary>selectMany action failed.</summary>
    public const string SelectMany = ProtocolMethods.SelectMany;

    /// <summary>selectMenu action failed.</summary>
    public const string SelectMenu = ProtocolMethods.SelectMenu;

    /// <summary>selectTree action failed.</summary>
    public const string SelectTree = ProtocolMethods.SelectTree;

    /// <summary>expand action failed.</summary>
    public const string Expand = ProtocolMethods.Expand;

    /// <summary>collapse action failed.</summary>
    public const string Collapse = ProtocolMethods.Collapse;

    /// <summary>Expect on element name failed or timed out.</summary>
    public const string ExpectName = "expectName";

    /// <summary>Expect on element selected state failed or timed out.</summary>
    public const string ExpectSelected = "expectSelected";

    /// <summary>Expect on element expanded state failed or timed out.</summary>
    public const string ExpectExpanded = "expectExpanded";

    /// <summary>Expect on element checked state failed or timed out.</summary>
    public const string ExpectChecked = "expectChecked";

    /// <summary>Expect on element enabled state failed or timed out.</summary>
    public const string ExpectEnabled = "expectEnabled";

    /// <summary>Expect on element visible state failed or timed out.</summary>
    public const string ExpectVisible = "expectVisible";

    /// <summary>Expect on element focused state failed or timed out.</summary>
    public const string ExpectFocused = "expectFocused";

    /// <summary>Expect on element name substring failed or timed out.</summary>
    public const string ExpectNameContains = "expectNameContains";

    /// <summary>Expect on element name regex failed or timed out.</summary>
    public const string ExpectNameMatches = "expectNameMatches";

    /// <summary>Expect on element value failed or timed out.</summary>
    public const string ExpectValue = "expectValue";

    /// <summary>Expect on element open ToolTip text failed or timed out.</summary>
    public const string ExpectToolTip = "expectToolTip";

    /// <summary>Wait until element is present failed or timed out.</summary>
    public const string WaitFor = "waitFor";

    /// <summary>Wait until element is gone / not visible failed or timed out.</summary>
    public const string ExpectGone = "expectGone";

    /// <summary>getCellText action failed.</summary>
    public const string GetCellText = ProtocolMethods.GetCellText;

    /// <summary>setCellValue action failed.</summary>
    public const string SetCellValue = ProtocolMethods.SetCellValue;

    /// <summary>selectCell action failed.</summary>
    public const string SelectCell = ProtocolMethods.SelectCell;

    /// <summary>selectRow action failed.</summary>
    public const string SelectRow = ProtocolMethods.SelectRow;

    /// <summary>clickColumnHeader action failed.</summary>
    public const string ClickColumnHeader = ProtocolMethods.ClickColumnHeader;

    /// <summary>addRow action failed.</summary>
    public const string AddRow = ProtocolMethods.AddRow;

    /// <summary>deleteSelectedRows action failed.</summary>
    public const string DeleteSelectedRows = ProtocolMethods.DeleteSelectedRows;

    /// <summary>Expect on DataGrid cell text failed or timed out.</summary>
    public const string ExpectCellText = "expectCellText";

    /// <summary>armOpenFile failed.</summary>
    public const string ArmOpenFile = ProtocolMethods.ArmOpenFile;

    /// <summary>armOpenFileCancel failed.</summary>
    public const string ArmOpenFileCancel = ProtocolMethods.ArmOpenFileCancel;

    /// <summary>armSaveFile failed.</summary>
    public const string ArmSaveFile = ProtocolMethods.ArmSaveFile;

    /// <summary>armSaveFileCancel failed.</summary>
    public const string ArmSaveFileCancel = ProtocolMethods.ArmSaveFileCancel;

    /// <summary>armOpenFolder failed.</summary>
    public const string ArmOpenFolder = ProtocolMethods.ArmOpenFolder;

    /// <summary>armOpenFolderCancel failed.</summary>
    public const string ArmOpenFolderCancel = ProtocolMethods.ArmOpenFolderCancel;

    /// <summary>armMessageBox failed.</summary>
    public const string ArmMessageBox = ProtocolMethods.ArmMessageBox;

    /// <summary>listWindows failed.</summary>
    public const string ListWindows = ProtocolMethods.ListWindows;

    /// <summary>switchWindow failed.</summary>
    public const string SwitchWindow = ProtocolMethods.SwitchWindow;

    /// <summary>Wait for a window timed out or failed.</summary>
    public const string WaitForWindow = "waitForWindow";

    /// <summary>Wait for a window to close timed out or failed.</summary>
    public const string WaitForWindowClosed = "waitForWindowClosed";

    /// <summary>Invoke that may open a window failed or timed out waiting for a new window.</summary>
    public const string InvokeOpeningWindow = ProtocolMethods.InvokeOpeningWindow;

    /// <summary>Wire method for the session handshake. Not a Scenario action.</summary>
    public const string Handshake = ProtocolMethods.Handshake;

    /// <summary>Wire method for reading the visual tree. Not a Scenario action.</summary>
    public const string GetTree = ProtocolMethods.GetTree;

    /// <summary>Soft-assert scope disposed with one or more collected failures.</summary>
    public const string SoftAssert = "softAssert";
}
