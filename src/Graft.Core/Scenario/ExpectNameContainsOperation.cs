namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree name contains a substring.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Substring">Expected ordinal substring.</param>
public sealed record ExpectNameContainsOperation(string AutomationId, string Substring) : ScenarioOperation(ScenarioActions.ExpectNameContains);
