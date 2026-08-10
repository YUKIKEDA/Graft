using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific DataGrid cell text read/write by row and column index or Header key.
/// </summary>
public interface IElementCellAccessor
{
    /// <summary>
    /// Returns the display text of the cell at <paramref name="row"/> on the DataGrid matched by
    /// <paramref name="selector"/>. Exactly one of <paramref name="column"/> /
    /// <paramref name="columnKey"/> must be set.
    /// </summary>
    /// <param name="selector">DataGrid selector (automationId required).</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index, or <see langword="null"/> when using key.</param>
    /// <param name="columnKey">Column Header string, or <see langword="null"/> when using index.</param>
    /// <returns>Cell display text (CheckBox: <c>True</c>/<c>False</c>).</returns>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Unsupported column or read failed.</exception>
    string GetCellText(ElementSelector selector, int row, int? column, string? columnKey);

    /// <summary>
    /// Edits the cell at <paramref name="row"/> via BeginEdit → value → CommitEdit.
    /// Exactly one of <paramref name="column"/> / <paramref name="columnKey"/> must be set.
    /// </summary>
    /// <param name="selector">DataGrid selector (automationId required).</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index, or <see langword="null"/> when using key.</param>
    /// <param name="columnKey">Column Header string, or <see langword="null"/> when using index.</param>
    /// <param name="value">Replacement text (CheckBox: <c>True</c>/<c>False</c>).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Read-only / unsupported column or edit failed.</exception>
    void SetCellValue(
        ElementSelector selector,
        int row,
        int? column,
        string? columnKey,
        string value
    );
}

#endif
