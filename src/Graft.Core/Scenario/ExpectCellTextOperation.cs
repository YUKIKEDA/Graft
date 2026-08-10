namespace Graft.Core.Scenario;

/// <summary>
/// Expect a DataGrid cell display text by row/column index.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index.</param>
/// <param name="Text">Expected cell text.</param>
public sealed record ExpectCellTextOperation(string AutomationId, int Row, int Column, string Text)
    : ScenarioOperation(ScenarioActions.ExpectCellText);
