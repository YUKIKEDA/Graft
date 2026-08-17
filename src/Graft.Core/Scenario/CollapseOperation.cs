namespace Graft.Core.Scenario;

/// <summary>
/// Collapse an element by automation id.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record CollapseOperation(string AutomationId) : ScenarioOperation(ScenarioActions.Collapse);
