namespace Graft.Core.Scenario;

/// <summary>
/// Expand an element by automation id.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record ExpandOperation(string AutomationId)
    : ScenarioOperation(ScenarioActions.Expand);
