namespace Graft.Core.Scenario;

/// <summary>
/// Wait until an element is present in the visual tree.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record WaitForOperation(string AutomationId) : ScenarioOperation(ScenarioActions.WaitFor);
