namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: right-click an element.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record RightClickOperation(string AutomationId)
    : ScenarioOperation(ScenarioActions.RightClick);
