namespace Graft.Core.Diagnostics;

/// <summary>
/// Stable <see cref="FailureReport.Step"/> vocabulary for Fluent / Scenario failures.
/// </summary>
public static class FailureSteps
{
    /// <summary>Wait until present / actionable timed out or failed.</summary>
    public const string Wait = "wait";

    /// <summary>Invoke (click) action failed.</summary>
    public const string Invoke = "invoke";

    /// <summary>rightClick action failed.</summary>
    public const string RightClick = "rightClick";

    /// <summary>doubleClick action failed.</summary>
    public const string DoubleClick = "doubleClick";

    /// <summary>hover action failed.</summary>
    public const string Hover = "hover";

    /// <summary>drag action failed.</summary>
    public const string Drag = "drag";

    /// <summary>clickAt action failed.</summary>
    public const string ClickAt = "clickAt";

    /// <summary>wheel action failed.</summary>
    public const string Wheel = "wheel";

    /// <summary>setValue action failed.</summary>
    public const string SetValue = "setValue";

    /// <summary>toggle action failed.</summary>
    public const string Toggle = "toggle";

    /// <summary>sendKeys action failed.</summary>
    public const string SendKeys = "sendKeys";

    /// <summary>pressKeys action failed.</summary>
    public const string PressKeys = "pressKeys";

    /// <summary>screenshot action failed.</summary>
    public const string Screenshot = "screenshot";

    /// <summary>scrollIntoView action failed.</summary>
    public const string ScrollIntoView = "scrollIntoView";

    /// <summary>select action failed.</summary>
    public const string Select = "select";

    /// <summary>selectMany action failed.</summary>
    public const string SelectMany = "selectMany";

    /// <summary>selectMenu action failed.</summary>
    public const string SelectMenu = "selectMenu";

    /// <summary>selectTree action failed.</summary>
    public const string SelectTree = "selectTree";

    /// <summary>expand action failed.</summary>
    public const string Expand = "expand";

    /// <summary>collapse action failed.</summary>
    public const string Collapse = "collapse";

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

    /// <summary>Expect on element name substring failed or timed out.</summary>
    public const string ExpectNameContains = "expectNameContains";

    /// <summary>Expect on element name regex failed or timed out.</summary>
    public const string ExpectNameMatches = "expectNameMatches";

    /// <summary>Expect on element value failed or timed out.</summary>
    public const string ExpectValue = "expectValue";

    /// <summary>Wait until element is present failed or timed out.</summary>
    public const string WaitFor = "waitFor";

    /// <summary>Wait until element is gone / not visible failed or timed out.</summary>
    public const string ExpectGone = "expectGone";

    /// <summary>getCellText action failed.</summary>
    public const string GetCellText = "getCellText";

    /// <summary>setCellValue action failed.</summary>
    public const string SetCellValue = "setCellValue";

    /// <summary>Expect on DataGrid cell text failed or timed out.</summary>
    public const string ExpectCellText = "expectCellText";

    /// <summary>armOpenFile failed.</summary>
    public const string ArmOpenFile = "armOpenFile";

    /// <summary>armOpenFileCancel failed.</summary>
    public const string ArmOpenFileCancel = "armOpenFileCancel";

    /// <summary>armSaveFile failed.</summary>
    public const string ArmSaveFile = "armSaveFile";

    /// <summary>armSaveFileCancel failed.</summary>
    public const string ArmSaveFileCancel = "armSaveFileCancel";

    /// <summary>armOpenFolder failed.</summary>
    public const string ArmOpenFolder = "armOpenFolder";

    /// <summary>armOpenFolderCancel failed.</summary>
    public const string ArmOpenFolderCancel = "armOpenFolderCancel";

    /// <summary>armMessageBox failed.</summary>
    public const string ArmMessageBox = "armMessageBox";

    /// <summary>listWindows failed.</summary>
    public const string ListWindows = "listWindows";

    /// <summary>switchWindow failed.</summary>
    public const string SwitchWindow = "switchWindow";

    /// <summary>Wait for a window timed out or failed.</summary>
    public const string WaitForWindow = "waitForWindow";

    /// <summary>Wait for a window to close timed out or failed.</summary>
    public const string WaitForWindowClosed = "waitForWindowClosed";

    /// <summary>invokeOpeningWindow failed or timed out waiting for a new window.</summary>
    public const string InvokeOpeningWindow = "invokeOpeningWindow";
}
