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

    /// <summary>Replace an element's value.</summary>
    public const string SetValue = FailureSteps.SetValue;

    /// <summary>Toggle an element (e.g. CheckBox).</summary>
    public const string Toggle = FailureSteps.Toggle;

    /// <summary>Type literal text into an element.</summary>
    public const string SendKeys = FailureSteps.SendKeys;

    /// <summary>Scroll an element or list item into view.</summary>
    public const string ScrollIntoView = FailureSteps.ScrollIntoView;

    /// <summary>Select a list/combo item by index.</summary>
    public const string Select = FailureSteps.Select;

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

    /// <summary>Read a DataGrid cell display text.</summary>
    public const string GetCellText = FailureSteps.GetCellText;

    /// <summary>Set a DataGrid Text cell value.</summary>
    public const string SetCellValue = FailureSteps.SetCellValue;

    /// <summary>Expect a DataGrid cell display text.</summary>
    public const string ExpectCellText = FailureSteps.ExpectCellText;

    /// <summary>List open windows.</summary>
    public const string ListWindows = FailureSteps.ListWindows;

    /// <summary>Switch the agent target window.</summary>
    public const string SwitchWindow = FailureSteps.SwitchWindow;

    /// <summary>Wait for a window by title and/or automation id.</summary>
    public const string WaitForWindow = FailureSteps.WaitForWindow;

    /// <summary>Invoke an element that may open a window (modal-safe).</summary>
    public const string InvokeOpeningWindow = FailureSteps.InvokeOpeningWindow;
}
