namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>value</c>.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Value">Expected tree value.</param>
public sealed record ExpectValueOperation(string AutomationId, string Value)
    : ScenarioOperation(ScenarioActions.ExpectValue);
