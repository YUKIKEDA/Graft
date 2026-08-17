namespace Graft.Core.Scenario;

/// <summary>
/// Invoke an element by automation id.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record InvokeOperation(string AutomationId) : ScenarioOperation(ScenarioActions.Invoke);
