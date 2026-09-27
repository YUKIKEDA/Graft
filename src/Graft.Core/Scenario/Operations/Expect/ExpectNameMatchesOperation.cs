namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree name matches a regex pattern.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Pattern">.NET regular expression pattern.</param>
public sealed record ExpectNameMatchesOperation(string AutomationId, string Pattern) : ScenarioOperation(ScenarioActions.ExpectNameMatches);
