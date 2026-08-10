using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific DataGrid cell text read/write by row/column index.
/// </summary>
public interface IElementCellAccessor
{
    /// <summary>
    /// Returns the display text of the Text cell at <paramref name="row"/> /
    /// <paramref name="column"/> on the DataGrid matched by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">DataGrid selector (automationId required).</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <returns>Cell display text.</returns>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Unsupported column or read failed.</exception>
    string GetCellText(ElementSelector selector, int row, int column);

    /// <summary>
    /// Edits the Text cell at <paramref name="row"/> / <paramref name="column"/> via
    /// BeginEdit → value → CommitEdit.
    /// </summary>
    /// <param name="selector">DataGrid selector (automationId required).</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    /// <param name="value">Replacement text.</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Read-only / unsupported column or edit failed.</exception>
    void SetCellValue(ElementSelector selector, int row, int column, string value);
}

#endif
