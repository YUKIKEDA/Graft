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

    /// <summary>Expect an element's tree name.</summary>
    public const string ExpectName = FailureSteps.ExpectName;
}
