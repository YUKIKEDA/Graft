namespace Graft.Core.Scenario;

/// <summary>
/// Toggle an element by automation id.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record ToggleOperation(string AutomationId)
    : ScenarioOperation(ScenarioActions.Toggle);
