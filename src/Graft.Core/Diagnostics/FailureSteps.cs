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

    /// <summary>Expect on element name failed or timed out.</summary>
    public const string ExpectName = "expectName";
}
