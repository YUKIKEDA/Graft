namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: press one keyboard chord on an element.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Keys">Chord DSL (e.g. <c>Control+A</c>).</param>
public sealed record PressKeysOperation(string AutomationId, string Keys) : ScenarioOperation(ScenarioActions.PressKeys);
