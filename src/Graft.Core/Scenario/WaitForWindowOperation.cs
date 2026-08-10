namespace Graft.Core.Scenario;

/// <summary>
/// Waits for a window by title and/or automation id.
/// </summary>
/// <param name="Title">Optional exact title.</param>
/// <param name="AutomationId">Optional exact automation id.</param>
/// <param name="SwitchTo">When true, switches the target to the matched window.</param>
public sealed record WaitForWindowOperation(string? Title, string? AutomationId, bool SwitchTo)
    : ScenarioOperation(ScenarioActions.WaitForWindow);
