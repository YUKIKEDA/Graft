namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: double-click an element (SendInput).
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record DoubleClickOperation(string AutomationId) : ScenarioOperation(ScenarioActions.DoubleClick);
