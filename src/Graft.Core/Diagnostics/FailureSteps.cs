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

    /// <summary>setValue action failed.</summary>
    public const string SetValue = "setValue";

    /// <summary>toggle action failed.</summary>
    public const string Toggle = "toggle";

    /// <summary>sendKeys action failed.</summary>
    public const string SendKeys = "sendKeys";

    /// <summary>scrollIntoView action failed.</summary>
    public const string ScrollIntoView = "scrollIntoView";

    /// <summary>select action failed.</summary>
    public const string Select = "select";

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

    /// <summary>getCellText action failed.</summary>
    public const string GetCellText = "getCellText";

    /// <summary>setCellValue action failed.</summary>
    public const string SetCellValue = "setCellValue";

    /// <summary>Expect on DataGrid cell text failed or timed out.</summary>
    public const string ExpectCellText = "expectCellText";

    /// <summary>listWindows failed.</summary>
    public const string ListWindows = "listWindows";

    /// <summary>switchWindow failed.</summary>
    public const string SwitchWindow = "switchWindow";

    /// <summary>Wait for a window timed out or failed.</summary>
    public const string WaitForWindow = "waitForWindow";

    /// <summary>invokeOpeningWindow failed or timed out waiting for a new window.</summary>
    public const string InvokeOpeningWindow = "invokeOpeningWindow";
}
