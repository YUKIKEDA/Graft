namespace Graft.Core.Scenario;

/// <summary>
/// Replace ListBox multi-selection by indexes (empty clears).
/// </summary>
/// <param name="AutomationId">ListBox automation id.</param>
/// <param name="Indexes">Zero-based item indexes.</param>
public sealed record SelectManyOperation(string AutomationId, IReadOnlyList<int> Indexes)
    : ScenarioOperation(ScenarioActions.SelectMany);
