namespace Graft.Core.Scenario;

/// <summary>
/// setValue on an element by automation id.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Value">Replacement text.</param>
public sealed record SetValueOperation(string AutomationId, string Value)
    : ScenarioOperation(ScenarioActions.SetValue);
