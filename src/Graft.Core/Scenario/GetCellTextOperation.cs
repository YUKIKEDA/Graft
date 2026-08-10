namespace Graft.Core.Scenario;

/// <summary>
/// Read a DataGrid cell display text by row/column index.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index.</param>
public sealed record GetCellTextOperation(string AutomationId, int Row, int Column)
    : ScenarioOperation(ScenarioActions.GetCellText);
