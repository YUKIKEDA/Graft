using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific DataGrid selection / sort / row CRUD (Phase 28).
/// </summary>
public interface IDataGridOperator
{
    /// <summary>
    /// Selects a single cell. Requires <c>SelectionUnit</c> Cell or CellOrRowHeader.
    /// Exactly one of <paramref name="column"/> / <paramref name="columnKey"/> must be set.
    /// </summary>
    /// <param name="selector">DataGrid selector.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index, or <see langword="null"/> when using key.</param>
    /// <param name="columnKey">Column Header string, or <see langword="null"/> when using index.</param>
    void SelectCell(ElementSelector selector, int row, int? column, string? columnKey);

    /// <summary>
    /// Selects the FullRow / SelectedItem whose cell at <paramref name="columnKey"/> equals
    /// <paramref name="value"/> (ordinal). Multiple matches → ambiguous.
    /// </summary>
    /// <param name="selector">DataGrid selector.</param>
    /// <param name="columnKey">Column Header string.</param>
    /// <param name="value">Exact cell display text.</param>
    void SelectRow(ElementSelector selector, string columnKey, string value);

    /// <summary>
    /// Clicks the column header for <paramref name="columnKey"/> (user sort UI).
    /// </summary>
    /// <param name="selector">DataGrid selector.</param>
    /// <param name="columnKey">Column Header string.</param>
    void ClickColumnHeader(ElementSelector selector, string columnKey);

    /// <summary>
    /// Adds a new row (IEditableCollectionView.AddNew or collection Add).
    /// </summary>
    /// <param name="selector">DataGrid selector.</param>
    void AddRow(ElementSelector selector);

    /// <summary>
    /// Deletes currently selected rows from the ItemsSource collection.
    /// </summary>
    /// <param name="selector">DataGrid selector.</param>
    void DeleteSelectedRows(ElementSelector selector);
}

#endif
