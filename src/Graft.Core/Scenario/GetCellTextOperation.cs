namespace Graft.Core.Scenario;

/// <summary>
/// Read a DataGrid cell (result is not asserted; use expectCellText to verify).
/// Exactly one of <see cref="Column"/> / <see cref="ColumnKey"/> is set.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index, or <see langword="null"/> when using key.</param>
/// <param name="ColumnKey">Column Header string, or <see langword="null"/> when using index.</param>
public sealed record GetCellTextOperation(
    string AutomationId,
    int Row,
    int? Column,
    string? ColumnKey
) : ScenarioOperation(ScenarioActions.GetCellText);
