namespace Graft.Core.Scenario;

/// <summary>
/// Expect DataGrid cell display text. Exactly one of <see cref="Column"/> / <see cref="ColumnKey"/> is set.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index, or <see langword="null"/> when using key.</param>
/// <param name="ColumnKey">Column Header string, or <see langword="null"/> when using index.</param>
/// <param name="Text">Expected cell text.</param>
public sealed record ExpectCellTextOperation(
    string AutomationId,
    int Row,
    int? Column,
    string? ColumnKey,
    string Text
) : ScenarioOperation(ScenarioActions.ExpectCellText);
