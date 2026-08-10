namespace Graft.Core.Scenario;

/// <summary>
/// Set a DataGrid Text cell value by row/column index.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index.</param>
/// <param name="Value">Replacement text.</param>
public sealed record SetCellValueOperation(string AutomationId, int Row, int Column, string Value)
    : ScenarioOperation(ScenarioActions.SetCellValue);
