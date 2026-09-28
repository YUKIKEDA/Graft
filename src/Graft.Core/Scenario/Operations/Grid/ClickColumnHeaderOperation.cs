using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: click a DataGrid column header (sort UI).
/// </summary>
/// <param name="Target">DataGrid automation id.</param>
/// <param name="ColumnKey">Column Header string.</param>
public sealed record ClickColumnHeaderOperation(Selector Target, string ColumnKey) : ScenarioOperation(ScenarioActions.ClickColumnHeader);
