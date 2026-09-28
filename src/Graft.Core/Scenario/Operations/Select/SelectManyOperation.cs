using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Replace ListBox or DataGrid multi-selection by indexes (empty clears).
/// </summary>
/// <param name="Target">ListBox or DataGrid automation id.</param>
/// <param name="Indexes">Zero-based item/row indexes.</param>
public sealed record SelectManyOperation(Selector Target, IReadOnlyList<int> Indexes) : ScenarioOperation(ScenarioActions.SelectMany);
