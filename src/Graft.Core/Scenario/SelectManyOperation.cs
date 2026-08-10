namespace Graft.Core.Scenario;

/// <summary>
/// Replace ListBox or DataGrid multi-selection by indexes (empty clears).
/// </summary>
/// <param name="AutomationId">ListBox or DataGrid automation id.</param>
/// <param name="Indexes">Zero-based item/row indexes.</param>
public sealed record SelectManyOperation(string AutomationId, IReadOnlyList<int> Indexes)
    : ScenarioOperation(ScenarioActions.SelectMany);
