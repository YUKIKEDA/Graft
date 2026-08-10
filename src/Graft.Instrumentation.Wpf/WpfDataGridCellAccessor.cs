using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Reads/writes WPF <see cref="DataGrid"/> Text/CheckBox cells by row and column index or Header.
/// </summary>
internal sealed class WpfDataGridCellAccessor : IElementCellAccessor
{
    /// <inheritdoc />
    public string GetCellText(ElementSelector selector, int row, int? column, string? columnKey)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return InvokeOnUi(() => GetCellTextOnUiThread(selector, row, column, columnKey));
    }

    /// <inheritdoc />
    public void SetCellValue(
        ElementSelector selector,
        int row,
        int? column,
        string? columnKey,
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(value);
        InvokeOnUi(() =>
        {
            SetCellValueOnUiThread(selector, row, column, columnKey, value);
            return 0;
        });
    }

    private static string GetCellTextOnUiThread(
        ElementSelector selector,
        int row,
        int? column,
        string? columnKey
    )
    {
        var dataGrid = ResolveDataGrid(selector);
        var columnIndex = ResolveColumnIndex(dataGrid, column, columnKey);
        EnsureRowIndex(dataGrid, row);
        var dataColumn = dataGrid.Columns[columnIndex];
        _ = WpfElementScroller.ScrollListItem(dataGrid, row);
        var rowContainer = RequireRow(dataGrid, row);
        dataGrid.ScrollIntoView(dataGrid.Items[row], dataColumn);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        var content = dataColumn.GetCellContent(rowContainer);
        if (content is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Failed to get cell content at row {row}, column {columnIndex}."
            );
        }

        return dataColumn switch
        {
            DataGridTextColumn => ReadDisplayText(content),
            DataGridCheckBoxColumn => ReadCheckBoxText(content),
            DataGridTemplateColumn => ReadTemplateText(content),
            _ => throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Column {columnIndex} is '{dataColumn.GetType().Name}'; only DataGridTextColumn, DataGridCheckBoxColumn, and DataGridTemplateColumn are supported."
            ),
        };
    }

    private static void SetCellValueOnUiThread(
        ElementSelector selector,
        int row,
        int? column,
        string? columnKey,
        string value
    )
    {
        var dataGrid = ResolveDataGrid(selector);
        var columnIndex = ResolveColumnIndex(dataGrid, column, columnKey);
        EnsureRowIndex(dataGrid, row);
        var dataColumn = dataGrid.Columns[columnIndex];

        if (dataGrid.IsReadOnly || dataColumn.IsReadOnly)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"DataGrid cell at column {columnIndex} is read-only."
            );
        }

        if (!dataGrid.IsEnabled || !dataGrid.IsVisible)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"DataGrid '{selector.AutomationId}' is not actionable (enabled={dataGrid.IsEnabled}, visible={dataGrid.IsVisible})."
            );
        }

        _ = WpfElementScroller.ScrollListItem(dataGrid, row);
        var item = dataGrid.Items[row]!;
        dataGrid.ScrollIntoView(item, dataColumn);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        dataGrid.CurrentCell = new DataGridCellInfo(item, dataColumn);
        if (!dataGrid.BeginEdit())
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"BeginEdit failed at row {row}, column {columnIndex}."
            );
        }

        dataGrid.UpdateLayout();
        Idle(dataGrid);

        try
        {
            var rowContainer = RequireRow(dataGrid, row);
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

            if (
                !dataGrid.CommitEdit(DataGridEditingUnit.Cell, true)
                || !dataGrid.CommitEdit(DataGridEditingUnit.Row, true)
            )
            {
                dataGrid.CancelEdit();
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"CommitEdit failed at row {row}, column {columnIndex}."
                );
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

        Idle(dataGrid);
    }

    private static void SetTextCell(
        FrameworkElement? content,
        string value,
        int row,
        int columnIndex,
        DataGrid dataGrid
    )
    {
        var textBox = content as TextBox ?? FindVisualChild<TextBox>(content);
        if (textBox is null)
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"No TextBox editor found at row {row}, column {columnIndex}."
            );
        }

        textBox.Text = value;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private static void SetCheckBoxCell(
        FrameworkElement? content,
        string value,
        int row,
        int columnIndex,
        DataGrid dataGrid
    )
    {
        if (!TryParseCheckBoxValue(value, out var isChecked))
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"setCellValue for CheckBox requires 'True' or 'False' (got '{value}')."
            );
        }

        var checkBox = content as CheckBox ?? FindVisualChild<CheckBox>(content);
        if (checkBox is null)
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"No CheckBox editor found at row {row}, column {columnIndex}."
            );
        }

        checkBox.IsChecked = isChecked;
        checkBox.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateSource();
    }

    private static void SetTemplateCell(
        FrameworkElement? content,
        string value,
        int row,
        int columnIndex,
        DataGrid dataGrid
    )
    {
        var textBox = content as TextBox ?? FindVisualChild<TextBox>(content);
        if (textBox is not null)
        {
            textBox.Text = value;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            return;
        }

        var checkBox = content as CheckBox ?? FindVisualChild<CheckBox>(content);
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

    private static string ReadTemplateText(FrameworkElement content)
    {
        var checkBox = content as CheckBox ?? FindVisualChild<CheckBox>(content);
        if (checkBox is not null && FindVisualChild<TextBlock>(content) is null)
        {
            return checkBox.IsChecked == true ? "True" : "False";
        }

        return ReadDisplayText(content);
    }

    private static DataGrid ResolveDataGrid(ElementSelector selector)
    {
        var resolver =
            AgentServices.ElementResolver
            ?? throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "No element resolver is registered. Call WpfGraft.Use() before Agent.Start()."
            );

        var resolved = resolver.Resolve(selector);
        if (resolved.Target is not DataGrid dataGrid)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"getCellText/setCellValue requires a DataGrid (got {resolved.Target.GetType().Name})."
            );
        }

        return dataGrid;
    }

    private static int ResolveColumnIndex(DataGrid dataGrid, int? column, string? columnKey)
    {
        var hasColumn = column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(columnKey);
        if (hasColumn == hasKey)
        {
            throw new ElementActionException(
                GraftErrorCodes.SelectorInvalid,
                "Exactly one of params.column or params.columnKey is required."
            );
        }

        if (hasColumn)
        {
            var index = column!.Value;
            if (index < 0)
            {
                throw new ElementActionException(
                    GraftErrorCodes.SelectorInvalid,
                    "params.column must be >= 0."
                );
            }

            if (index >= dataGrid.Columns.Count)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotFound,
                    $"Column index {index} is out of range (count={dataGrid.Columns.Count})."
                );
            }

            return index;
        }

        var key = columnKey!.Trim();
        var matches = new List<int>();
        for (var i = 0; i < dataGrid.Columns.Count; i++)
        {
            if (
                string.Equals(
                    FormatHeader(dataGrid.Columns[i].Header),
                    key,
                    StringComparison.Ordinal
                )
            )
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotFound,
                $"No DataGrid column Header matched columnKey '{key}'."
            );
        }

        if (matches.Count > 1)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementAmbiguous,
                $"Multiple DataGrid columns matched columnKey '{key}' ({matches.Count})."
            );
        }

        return matches[0];
    }

    private static void EnsureRowIndex(DataGrid dataGrid, int row)
    {
        if (row < 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.SelectorInvalid,
                "params.row must be >= 0."
            );
        }

        if (row >= dataGrid.Items.Count)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotFound,
                $"Row index {row} is out of range (count={dataGrid.Items.Count})."
            );
        }
    }

    private static string FormatHeader(object? header) =>
        header switch
        {
            null => string.Empty,
            string text => text,
            _ => Convert.ToString(header, CultureInfo.InvariantCulture) ?? string.Empty,
        };

    private static DataGridRow RequireRow(DataGrid dataGrid, int row)
    {
        if (dataGrid.ItemContainerGenerator.ContainerFromIndex(row) is not DataGridRow rowContainer)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Failed to realize DataGrid row at index {row}."
            );
        }

        return rowContainer;
    }

    private static string ReadDisplayText(FrameworkElement content) =>
        content switch
        {
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            TextBox textBox => textBox.Text ?? string.Empty,
            _ => FindVisualChild<TextBlock>(content)?.Text
                ?? FindVisualChild<TextBox>(content)?.Text
                ?? content.ToString()
                ?? string.Empty,
        };

    private static string ReadCheckBoxText(FrameworkElement content)
    {
        var checkBox = content as CheckBox ?? FindVisualChild<CheckBox>(content);
        if (checkBox is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "Failed to read CheckBox cell content."
            );
        }

        return checkBox.IsChecked == true ? "True" : "False";
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

    private static T? FindVisualChild<T>(DependencyObject? parent)
        where T : DependencyObject
    {
        if (parent is null)
        {
            return null;
        }

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void Idle(DispatcherObject element) =>
        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);

    private static T InvokeOnUi<T>(Func<T> action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot access DataGrid cells."
            );
        }

        if (dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.Invoke(action, DispatcherPriority.Normal);
    }
}
