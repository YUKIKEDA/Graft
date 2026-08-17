namespace Graft.Core.Scenario;

/// <summary>
/// Set a DataGrid cell value. Exactly one of <see cref="Column"/> / <see cref="ColumnKey"/> is set.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index, or <see langword="null"/> when using key.</param>
/// <param name="ColumnKey">Column Header string, or <see langword="null"/> when using index.</param>
/// <param name="Value">Replacement text.</param>
public sealed record SetCellValueOperation(string AutomationId, int Row, int? Column, string? ColumnKey, string Value)
    : ScenarioOperation(ScenarioActions.SetCellValue);
