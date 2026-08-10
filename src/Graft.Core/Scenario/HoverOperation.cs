namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: hover over an element (SendInput).
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record HoverOperation(string AutomationId) : ScenarioOperation(ScenarioActions.Hover);
