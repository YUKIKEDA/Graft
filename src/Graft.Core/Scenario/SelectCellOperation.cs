namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: select a DataGrid cell by row and column index or Header key.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index (xor <paramref name="ColumnKey"/>).</param>
/// <param name="ColumnKey">Column Header key (xor <paramref name="Column"/>).</param>
public sealed record SelectCellOperation(
    string AutomationId,
    int Row,
    int? Column = null,
    string? ColumnKey = null
) : ScenarioOperation(ScenarioActions.SelectCell);
