using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Reads/writes WPF <see cref="DataGrid"/> Text column cells by row/column index.
/// </summary>
internal sealed class WpfDataGridCellAccessor : IElementCellAccessor
{
    /// <inheritdoc />
    public string GetCellText(ElementSelector selector, int row, int column)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return InvokeOnUi(() => GetCellTextOnUiThread(selector, row, column));
    }

    /// <inheritdoc />
    public void SetCellValue(ElementSelector selector, int row, int column, string value)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(value);
        InvokeOnUi(() =>
        {
            SetCellValueOnUiThread(selector, row, column, value);
            return 0;
        });
    }

    private static string GetCellTextOnUiThread(ElementSelector selector, int row, int column)
    {
        var dataGrid = ResolveDataGrid(selector);
        EnsureIndices(dataGrid, row, column);
        var textColumn = RequireTextColumn(dataGrid, column);
        _ = WpfElementScroller.ScrollListItem(dataGrid, row);
        var rowContainer = RequireRow(dataGrid, row);
        dataGrid.ScrollIntoView(dataGrid.Items[row], textColumn);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        var content = textColumn.GetCellContent(rowContainer);
        if (content is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Failed to get cell content at row {row}, column {column}."
            );
        }

        return ReadDisplayText(content);
    }

    private static void SetCellValueOnUiThread(
        ElementSelector selector,
        int row,
        int column,
        string value
    )
    {
        var dataGrid = ResolveDataGrid(selector);
        EnsureIndices(dataGrid, row, column);
        var textColumn = RequireTextColumn(dataGrid, column);

        if (dataGrid.IsReadOnly || textColumn.IsReadOnly)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"DataGrid cell at column {column} is read-only."
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
        dataGrid.ScrollIntoView(item, textColumn);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        dataGrid.CurrentCell = new DataGridCellInfo(item, textColumn);
        if (!dataGrid.BeginEdit())
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"BeginEdit failed at row {row}, column {column}."
            );
        }

        dataGrid.UpdateLayout();
        Idle(dataGrid);

        var rowContainer = RequireRow(dataGrid, row);
        var content = textColumn.GetCellContent(rowContainer);
        var textBox = content as TextBox ?? FindVisualChild<TextBox>(content);
        if (textBox is null)
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"No TextBox editor found at row {row}, column {column}."
            );
        }

        textBox.Text = value;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

        if (
            !dataGrid.CommitEdit(DataGridEditingUnit.Cell, true)
            || !dataGrid.CommitEdit(DataGridEditingUnit.Row, true)
        )
        {
            dataGrid.CancelEdit();
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"CommitEdit failed at row {row}, column {column}."
            );
        }

        Idle(dataGrid);
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

    private static void EnsureIndices(DataGrid dataGrid, int row, int column)
    {
        if (row < 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.SelectorInvalid,
                "params.row must be >= 0."
            );
        }

        if (column < 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.SelectorInvalid,
                "params.column must be >= 0."
            );
        }

        if (row >= dataGrid.Items.Count)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotFound,
                $"Row index {row} is out of range (count={dataGrid.Items.Count})."
            );
        }

        if (column >= dataGrid.Columns.Count)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotFound,
                $"Column index {column} is out of range (count={dataGrid.Columns.Count})."
            );
        }
    }

    private static DataGridTextColumn RequireTextColumn(DataGrid dataGrid, int column)
    {
        if (dataGrid.Columns[column] is not DataGridTextColumn textColumn)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Column {column} is '{dataGrid.Columns[column].GetType().Name}'; only DataGridTextColumn is supported."
            );
        }

        return textColumn;
    }

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
