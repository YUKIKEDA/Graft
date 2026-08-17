namespace Graft.Core.Scenario;

/// <summary>
/// Wait until an element is not found or not visible.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record ExpectGoneOperation(string AutomationId) : ScenarioOperation(ScenarioActions.ExpectGone);
