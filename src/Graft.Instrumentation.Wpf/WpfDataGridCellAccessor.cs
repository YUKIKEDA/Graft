using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Reads/writes WPF <see cref="DataGrid"/> cells and reads <see cref="ListView"/>/<see cref="GridView"/> cells
/// by row and column index or Header.
/// </summary>
internal sealed class WpfDataGridCellAccessor : IElementCellAccessor
{
    private readonly IElementResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfDataGridCellAccessor"/> class.
    /// </summary>
    /// <param name="resolver">Element resolver from the agent backend.</param>
    public WpfDataGridCellAccessor(IElementResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <inheritdoc />
    public string GetCellText(ElementSelector selector, int row, int? column, string? columnKey)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return InvokeOnUi(() => GetCellTextOnUiThread(selector, row, column, columnKey));
    }

    /// <inheritdoc />
    public void SetCellValue(ElementSelector selector, int row, int? column, string? columnKey, string value)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(value);
        InvokeOnUi(() =>
        {
            SetCellValueOnUiThread(selector, row, column, columnKey, value);
            return 0;
        });
    }

    private string GetCellTextOnUiThread(ElementSelector selector, int row, int? column, string? columnKey)
    {
        var host = ResolveCellHost(selector);
        if (host is ListView listView)
        {
            return GetListViewCellText(listView, row, column, columnKey);
        }

        var dataGrid = (DataGrid)host;
        var columnIndex = WpfDataGridCells.ResolveColumnIndex(dataGrid, column, columnKey);
        WpfDataGridCells.EnsureRowIndex(dataGrid, row);
        var dataColumn = dataGrid.Columns[columnIndex];
        _ = WpfElementScroller.ScrollListItem(dataGrid, row);
        var rowContainer = WpfDataGridCells.RequireRow(dataGrid, row);
        dataGrid.ScrollIntoView(dataGrid.Items[row], dataColumn);
        dataGrid.UpdateLayout();
        WpfDispatch.Idle(dataGrid);

        var content = dataColumn.GetCellContent(rowContainer);
        if (content is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"Failed to get cell content at row {row}, column {columnIndex}.");
        }

        return dataColumn switch
        {
            DataGridTextColumn => WpfDataGridCells.ReadDisplayText(content),
            DataGridCheckBoxColumn => WpfDataGridCells.ReadCheckBoxText(content),
            DataGridTemplateColumn => WpfDataGridCells.ReadTemplateText(content),
            _ => throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Column {columnIndex} is '{dataColumn.GetType().Name}'; only DataGridTextColumn, DataGridCheckBoxColumn, and DataGridTemplateColumn are supported."
            ),
        };
    }

    private static string GetListViewCellText(ListView listView, int row, int? column, string? columnKey)
    {
        if (listView.View is not GridView gridView)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"getCellText for ListView requires a GridView (got {listView.View?.GetType().Name ?? "null"})."
            );
        }

        var columnIndex = ResolveGridViewColumnIndex(gridView, column, columnKey);
        EnsureListItemsIndex(listView, row);
        _ = WpfElementScroller.ScrollListItem(listView, row);
        listView.UpdateLayout();
        WpfDispatch.Idle(listView);

        var item = listView.Items[row];
        if (item is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"ListView row {row} is null.");
        }

        var gridColumn = gridView.Columns[columnIndex];
        if (gridColumn.DisplayMemberBinding is Binding binding)
        {
            return ReadBoundValue(item, binding);
        }

        if (listView.ItemContainerGenerator.ContainerFromIndex(row) is ListViewItem rowContainer)
        {
            listView.ScrollIntoView(item);
            listView.UpdateLayout();
            WpfDispatch.Idle(listView);
            var cells = WpfVisualTree.FindVisualChildren<TextBlock>(rowContainer).ToList();
            if (columnIndex < cells.Count)
            {
                return cells[columnIndex].Text ?? string.Empty;
            }
        }

        throw new ElementActionException(
            GraftErrorCodes.ActionFailed,
            $"Failed to read ListView cell at row {row}, column {columnIndex} (need DisplayMemberBinding or realized TextBlock)."
        );
    }

    private static string ReadBoundValue(object item, Binding binding)
    {
        var path = binding.Path?.Path;
        if (string.IsNullOrEmpty(path))
        {
            return Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        object? current = item;
        foreach (var segment in path.Split('.'))
        {
            if (current is null)
            {
                return string.Empty;
            }

            var property = current.GetType().GetProperty(segment);
            if (property is null)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"ListView binding path '{path}' could not resolve property '{segment}' on {current.GetType().Name}."
                );
            }

            current = property.GetValue(current);
        }

        return Convert.ToString(current, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void SetCellValueOnUiThread(ElementSelector selector, int row, int? column, string? columnKey, string value)
    {
        var host = ResolveCellHost(selector);
        if (host is ListView)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "setCellValue is not supported for ListView/GridView (read-only).");
        }

        var dataGrid = (DataGrid)host;
        WpfElementResolve.RequireActionable(dataGrid, selector.AutomationId ?? string.Empty, "DataGrid");

        var columnIndex = WpfDataGridCells.ResolveColumnIndex(dataGrid, column, columnKey);
        WpfDataGridCells.EnsureRowIndex(dataGrid, row);
        var dataColumn = dataGrid.Columns[columnIndex];

        if (dataGrid.IsReadOnly || dataColumn.IsReadOnly)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"DataGrid cell at column {columnIndex} is read-only.");
        }

        _ = WpfElementScroller.ScrollListItem(dataGrid, row);
        var item = dataGrid.Items[row]!;
        dataGrid.ScrollIntoView(item, dataColumn);
        dataGrid.UpdateLayout();
        WpfDispatch.Idle(dataGrid);

        dataGrid.CurrentCell = new DataGridCellInfo(item, dataColumn);
        if (!dataGrid.BeginEdit())
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"BeginEdit failed at row {row}, column {columnIndex}.");
        }

        dataGrid.UpdateLayout();
        WpfDispatch.Idle(dataGrid);

        try
        {
            var rowContainer = WpfDataGridCells.RequireRow(dataGrid, row);
            var content = dataColumn.GetCellContent(rowContainer);
            switch (dataColumn)
            {
                case DataGridTextColumn:
                    SetTextCell(content, value, row, columnIndex, dataGrid);
                    break;
                case DataGridCheckBoxColumn:
                    SetCheckBoxCell(content, value, row, columnIndex, dataGrid);
                    break;
                case DataGridTemplateColumn:
                    SetTemplateCell(content, value, row, columnIndex, dataGrid);
                    break;
                default:
                    dataGrid.CancelEdit();
                    throw new ElementActionException(
                        GraftErrorCodes.ActionFailed,
                        $"Column {columnIndex} is '{dataColumn.GetType().Name}'; only DataGridTextColumn, DataGridCheckBoxColumn, and DataGridTemplateColumn are supported."
                    );
            }

            if (!dataGrid.CommitEdit(DataGridEditingUnit.Cell, true) || !dataGrid.CommitEdit(DataGridEditingUnit.Row, true))
            {
                dataGrid.CancelEdit();
                throw new ElementActionException(GraftErrorCodes.ActionFailed, $"CommitEdit failed at row {row}, column {columnIndex}.");
            }
        }
        catch
        {
            try
            {
                dataGrid.CancelEdit();
            }
            catch
            {
                // Best-effort cancel.
            }

            throw;
        }

        WpfDispatch.Idle(dataGrid);
    }

    private static void SetTextCell(FrameworkElement? content, string value, int row, int columnIndex, DataGrid dataGrid)
    {
        var textBox = content as TextBox ?? WpfVisualTree.FindVisualChild<TextBox>(content);
        if (textBox is null)
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"No TextBox editor found at row {row}, column {columnIndex}.");
        }

        textBox.Text = value;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private static void SetCheckBoxCell(FrameworkElement? content, string value, int row, int columnIndex, DataGrid dataGrid)
    {
        if (!TryParseCheckBoxValue(value, out var isChecked))
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"setCellValue for CheckBox requires 'True' or 'False' (got '{value}').");
        }

        var checkBox = content as CheckBox ?? WpfVisualTree.FindVisualChild<CheckBox>(content);
        if (checkBox is null)
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"No CheckBox editor found at row {row}, column {columnIndex}.");
        }

        checkBox.IsChecked = isChecked;
        checkBox.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateSource();
    }

    private static void SetTemplateCell(FrameworkElement? content, string value, int row, int columnIndex, DataGrid dataGrid)
    {
        var textBox = content as TextBox ?? WpfVisualTree.FindVisualChild<TextBox>(content);
        if (textBox is not null)
        {
            textBox.Text = value;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            return;
        }

        var checkBox = content as CheckBox ?? WpfVisualTree.FindVisualChild<CheckBox>(content);
        if (checkBox is not null)
        {
            if (!TryParseCheckBoxValue(value, out var isChecked))
            {
                dataGrid.CancelEdit();
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"setCellValue for Template CheckBox requires 'True' or 'False' (got '{value}')."
                );
            }

            checkBox.IsChecked = isChecked;
            checkBox.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateSource();
            return;
        }

        dataGrid.CancelEdit();
        throw new ElementActionException(
            GraftErrorCodes.ActionFailed,
            $"Template column at row {row}, column {columnIndex} has no single TextBox/CheckBox editor."
        );
    }

    private FrameworkElement ResolveCellHost(ElementSelector selector)
    {
        var resolved = WpfElementResolve.Lookup(_resolver, selector);
        return resolved.Target switch
        {
            DataGrid dataGrid => dataGrid,
            ListView listView => listView,
            _ => throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"getCellText/setCellValue requires a DataGrid or ListView (got {resolved.Target.GetType().Name})."
            ),
        };
    }

    private static int ResolveGridViewColumnIndex(GridView gridView, int? column, string? columnKey)
    {
        var hasColumn = column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(columnKey);
        if (hasColumn == hasKey)
        {
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "Exactly one of params.column or params.columnKey is required.");
        }

        if (hasColumn)
        {
            var index = column!.Value;
            if (index < 0)
            {
                throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.column must be >= 0.");
            }

            if (index >= gridView.Columns.Count)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotFound,
                    $"Column index {index} is out of range (count={gridView.Columns.Count})."
                );
            }

            return index;
        }

        var key = columnKey!.Trim();
        var matches = new List<int>();
        for (var i = 0; i < gridView.Columns.Count; i++)
        {
            if (string.Equals(WpfDataGridCells.FormatHeader(gridView.Columns[i].Header), key, StringComparison.Ordinal))
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"No GridView column Header matched columnKey '{key}'.");
        }

        if (matches.Count > 1)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementAmbiguous,
                $"Multiple GridView columns matched columnKey '{key}' ({matches.Count})."
            );
        }

        return matches[0];
    }

    private static void EnsureListItemsIndex(ItemsControl itemsControl, int row)
    {
        if (row < 0)
        {
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.row must be >= 0.");
        }

        if (row >= itemsControl.Items.Count)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"Row index {row} is out of range (count={itemsControl.Items.Count}).");
        }
    }

    private static bool TryParseCheckBoxValue(string value, out bool isChecked)
    {
        if (string.Equals(value, "True", StringComparison.Ordinal))
        {
            isChecked = true;
            return true;
        }

        if (string.Equals(value, "False", StringComparison.Ordinal))
        {
            isChecked = false;
            return true;
        }

        isChecked = false;
        return false;
    }

    private static T InvokeOnUi<T>(Func<T> action) =>
        WpfDispatch.InvokeOnUi(action, "WPF Application.Current is not available; cannot access DataGrid cells.");
}
