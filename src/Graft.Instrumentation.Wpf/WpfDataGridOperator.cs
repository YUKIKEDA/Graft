using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// WPF DataGrid selection / sort header / row CRUD (Phase 28).
/// </summary>
internal sealed class WpfDataGridOperator : IDataGridOperator
{
    /// <inheritdoc />
    public void SelectCell(ElementSelector selector, int row, int? column, string? columnKey)
    {
        ArgumentNullException.ThrowIfNull(selector);
        InvokeOnUi(() =>
        {
            SelectCellOnUiThread(selector, row, column, columnKey);
            return 0;
        });
    }

    /// <inheritdoc />
    public void SelectRow(ElementSelector selector, string columnKey, string value)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        ArgumentNullException.ThrowIfNull(value);
        InvokeOnUi(() =>
        {
            SelectRowOnUiThread(selector, columnKey, value);
            return 0;
        });
    }

    /// <inheritdoc />
    public void ClickColumnHeader(ElementSelector selector, string columnKey)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        InvokeOnUi(() =>
        {
            ClickColumnHeaderOnUiThread(selector, columnKey);
            return 0;
        });
    }

    /// <inheritdoc />
    public void AddRow(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        InvokeOnUi(() =>
        {
            AddRowOnUiThread(selector);
            return 0;
        });
    }

    /// <inheritdoc />
    public void DeleteSelectedRows(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        InvokeOnUi(() =>
        {
            DeleteSelectedRowsOnUiThread(selector);
            return 0;
        });
    }

    private static void SelectCellOnUiThread(ElementSelector selector, int row, int? column, string? columnKey)
    {
        var dataGrid = ResolveActionableDataGrid(selector);
        if (dataGrid.SelectionUnit is not (DataGridSelectionUnit.Cell or DataGridSelectionUnit.CellOrRowHeader))
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"selectCell requires SelectionUnit Cell or CellOrRowHeader (got {dataGrid.SelectionUnit})."
            );
        }

        var columnIndex = ResolveColumnIndex(dataGrid, column, columnKey);
        EnsureRowIndex(dataGrid, row);
        var dataColumn = dataGrid.Columns[columnIndex];
        _ = WpfElementScroller.ScrollListItem(dataGrid, row);
        var item = dataGrid.Items[row]!;
        dataGrid.ScrollIntoView(item, dataColumn);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        dataGrid.SelectedCells.Clear();
        var cellInfo = new DataGridCellInfo(item, dataColumn);
        dataGrid.CurrentCell = cellInfo;
        dataGrid.SelectedCells.Add(cellInfo);
        Idle(dataGrid);
    }

    private static void SelectRowOnUiThread(ElementSelector selector, string columnKey, string value)
    {
        var dataGrid = ResolveActionableDataGrid(selector);
        var columnIndex = ResolveColumnIndex(dataGrid, column: null, columnKey);
        var dataColumn = dataGrid.Columns[columnIndex];
        var matches = new List<int>();

        for (var row = 0; row < dataGrid.Items.Count; row++)
        {
            _ = WpfElementScroller.ScrollListItem(dataGrid, row);
            dataGrid.ScrollIntoView(dataGrid.Items[row], dataColumn);
            dataGrid.UpdateLayout();
            Idle(dataGrid);

            var rowContainer = RequireRow(dataGrid, row);
            var content = dataColumn.GetCellContent(rowContainer);
            if (content is null)
            {
                continue;
            }

            var text = ReadCellDisplayText(dataColumn, content);
            if (string.Equals(text, value, StringComparison.Ordinal))
            {
                matches.Add(row);
            }
        }

        if (matches.Count == 0)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"No DataGrid row matched columnKey '{columnKey}' value '{value}'.");
        }

        if (matches.Count > 1)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementAmbiguous,
                $"Multiple DataGrid rows matched columnKey '{columnKey}' value '{value}' ({matches.Count})."
            );
        }

        var matchRow = matches[0];
        _ = WpfElementScroller.ScrollListItem(dataGrid, matchRow);
        var matchItem = dataGrid.Items[matchRow]!;
        dataGrid.ScrollIntoView(matchItem);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        // SelectedItems can only be mutated when SelectionMode is Extended.
        ClearDataGridSelection(dataGrid);
        dataGrid.SelectedItem = matchItem;
        dataGrid.SelectedIndex = matchRow;
        Idle(dataGrid);
    }

    private static void ClickColumnHeaderOnUiThread(ElementSelector selector, string columnKey)
    {
        var dataGrid = ResolveActionableDataGrid(selector);
        var columnIndex = ResolveColumnIndex(dataGrid, column: null, columnKey);
        dataGrid.UpdateLayout();
        Idle(dataGrid);

        var column = dataGrid.Columns[columnIndex];
        if (!column.CanUserSort || !dataGrid.CanUserSortColumns)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotActionable, $"Column '{columnKey}' is not user-sortable.");
        }

        if (string.IsNullOrEmpty(column.SortMemberPath))
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Column '{columnKey}' has no SortMemberPath; cannot clickColumnHeader-sort."
            );
        }

        // Emulate column-header click sort cycle: none → Asc → Desc → none.
        column.SortDirection = column.SortDirection switch
        {
            ListSortDirection.Ascending => ListSortDirection.Descending,
            ListSortDirection.Descending => null,
            _ => ListSortDirection.Ascending,
        };

        foreach (var other in dataGrid.Columns)
        {
            if (!ReferenceEquals(other, column))
            {
                other.SortDirection = null;
            }
        }

        var view =
            CollectionViewSource.GetDefaultView(dataGrid.ItemsSource)
            ?? throw new ElementActionException(GraftErrorCodes.ActionFailed, "DataGrid has no ICollectionView for sorting.");

        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            if (column.SortDirection is { } direction)
            {
                view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, direction));
            }
        }

        // Best-effort visual click for header chrome (does not replace the sort above).
        var header = FindColumnHeader(dataGrid, column);
        if (header is not null)
        {
            header.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent) { Source = header });
        }

        Idle(dataGrid);
    }

    private static void AddRowOnUiThread(ElementSelector selector)
    {
        var dataGrid = ResolveActionableDataGrid(selector);
        if (dataGrid.ItemsSource is IEditableCollectionView editable)
        {
            var added = editable.AddNew();
            editable.CommitNew();
            if (added is not null)
            {
                dataGrid.SelectedItem = added;
                dataGrid.ScrollIntoView(added);
            }

            Idle(dataGrid);
            return;
        }

        if (dataGrid.ItemsSource is IList list && !list.IsFixedSize && !list.IsReadOnly)
        {
            Type? itemType = null;
            if (dataGrid.ItemsSource is IEnumerable enumerable)
            {
                foreach (var existing in enumerable)
                {
                    itemType = existing?.GetType();
                    if (itemType is not null)
                    {
                        break;
                    }
                }
            }

            itemType ??= list.GetType().IsGenericType ? list.GetType().GetGenericArguments()[0] : null;

            if (itemType is null || itemType == typeof(object))
            {
                throw new ElementActionException(GraftErrorCodes.ActionFailed, "addRow could not infer item type for ItemsSource.");
            }

            var instance =
                Activator.CreateInstance(itemType)
                ?? throw new ElementActionException(GraftErrorCodes.ActionFailed, $"Failed to create instance of '{itemType.Name}' for addRow.");
            list.Add(instance);
            dataGrid.SelectedItem = instance;
            dataGrid.ScrollIntoView(instance);
            Idle(dataGrid);
            return;
        }

        throw new ElementActionException(GraftErrorCodes.ActionFailed, "addRow requires IEditableCollectionView or mutable IList ItemsSource.");
    }

    private static void DeleteSelectedRowsOnUiThread(ElementSelector selector)
    {
        var dataGrid = ResolveActionableDataGrid(selector);
        var selected = dataGrid.SelectedItems.Cast<object>().ToList();
        if (selected.Count == 0 && dataGrid.SelectedItem is { } single)
        {
            selected.Add(single);
        }

        if (selected.Count == 0)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "deleteSelectedRows requires at least one selected row.");
        }

        if (dataGrid.ItemsSource is not IList list || list.IsFixedSize || list.IsReadOnly)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "deleteSelectedRows requires a mutable IList ItemsSource.");
        }

        foreach (var item in selected)
        {
            list.Remove(item);
        }

        ClearDataGridSelection(dataGrid);
        dataGrid.SelectedItem = null;
        Idle(dataGrid);
    }

    /// <summary>
    /// Clears row selection without throwing on <see cref="DataGridSelectionMode.Single"/>
    /// (WPF forbids mutating <c>DataGrid.SelectedItems</c> in Single mode).
    /// </summary>
    private static void ClearDataGridSelection(DataGrid dataGrid)
    {
        if (dataGrid.SelectionMode == DataGridSelectionMode.Extended)
        {
            dataGrid.SelectedItems.Clear();
        }
        else
        {
            dataGrid.SelectedItem = null;
        }
    }

    private static DataGrid ResolveActionableDataGrid(ElementSelector selector)
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
                $"DataGrid operation requires a DataGrid (got {resolved.Target.GetType().Name})."
            );
        }

        if (!dataGrid.IsEnabled || !dataGrid.IsVisible)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"DataGrid '{selector.AutomationId}' is not actionable (enabled={dataGrid.IsEnabled}, visible={dataGrid.IsVisible})."
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
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "Exactly one of params.column or params.columnKey is required.");
        }

        if (hasColumn)
        {
            var index = column!.Value;
            if (index < 0)
            {
                throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.column must be >= 0.");
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
            if (string.Equals(FormatHeader(dataGrid.Columns[i].Header), key, StringComparison.Ordinal))
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"No DataGrid column Header matched columnKey '{key}'.");
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
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.row must be >= 0.");
        }

        if (row >= dataGrid.Items.Count)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"Row index {row} is out of range (count={dataGrid.Items.Count}).");
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
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"Failed to realize DataGrid row at index {row}.");
        }

        return rowContainer;
    }

    private static string ReadCellDisplayText(DataGridColumn column, FrameworkElement content) =>
        column switch
        {
            DataGridCheckBoxColumn => ReadCheckBoxText(content),
            DataGridTemplateColumn => ReadTemplateText(content),
            _ => ReadDisplayText(content),
        };

    private static string ReadDisplayText(FrameworkElement content) =>
        content switch
        {
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            TextBox textBox => textBox.Text ?? string.Empty,
            _ => FindVisualChild<TextBlock>(content)?.Text ?? FindVisualChild<TextBox>(content)?.Text ?? content.ToString() ?? string.Empty,
        };

    private static string ReadCheckBoxText(FrameworkElement content)
    {
        var checkBox = content as CheckBox ?? FindVisualChild<CheckBox>(content);
        if (checkBox is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "Failed to read CheckBox cell content.");
        }

        return checkBox.IsChecked == true ? "True" : "False";
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

    private static DataGridColumnHeader? FindColumnHeader(DataGrid dataGrid, DataGridColumn column)
    {
        return FindVisualChildren<DataGridColumnHeader>(dataGrid).FirstOrDefault(header => ReferenceEquals(header.Column, column));
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject? parent)
        where T : DependencyObject
    {
        if (parent is null)
        {
            return null;
        }

        foreach (var child in FindVisualChildren<T>(parent))
        {
            return child;
        }

        return null;
    }

    private static void Idle(DispatcherObject element) => element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);

    private static T InvokeOnUi<T>(Func<T> action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot operate DataGrid.");
        }

        if (dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.Invoke(action, DispatcherPriority.Normal);
    }
}
