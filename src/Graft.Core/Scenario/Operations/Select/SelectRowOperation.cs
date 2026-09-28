using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: select a DataGrid row by column Header key and cell value.
/// </summary>
/// <param name="Target">DataGrid automation id.</param>
/// <param name="ColumnKey">Column Header string.</param>
/// <param name="Value">Exact cell display text.</param>
public sealed record SelectRowOperation(Selector Target, string ColumnKey, string Value) : ScenarioOperation(ScenarioActions.SelectRow);
