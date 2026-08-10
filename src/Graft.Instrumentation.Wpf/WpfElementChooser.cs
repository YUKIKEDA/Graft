using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Tree;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Selects WPF list/combo/tab/DataGrid items (single or multi) on the UI dispatcher.
/// </summary>
internal sealed class WpfElementChooser : IElementChooser
{
    /// <inheritdoc />
    public void Select(ElementSelector selector, int index)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot select."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SelectOnUiThread(selector, index);
            return;
        }

        dispatcher.Invoke(() => SelectOnUiThread(selector, index), DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void SelectMany(ElementSelector selector, IReadOnlyList<int> indexes)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(indexes);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "WPF Application.Current is not available; cannot selectMany."
            );
        }

        if (dispatcher.CheckAccess())
        {
            SelectManyOnUiThread(selector, indexes);
            return;
        }

        dispatcher.Invoke(() => SelectManyOnUiThread(selector, indexes), DispatcherPriority.Normal);
    }

    private static void SelectOnUiThread(ElementSelector selector, int index)
    {
        var element = ResolveActionable(selector);

        switch (element)
        {
            case TabControl tab:
                SelectTab(tab, index);
                break;
            default:
                // Realize / scroll first (virtualized lists).
                _ = WpfElementScroller.ScrollListItem(element, index);

                // DataGrid : MultiSelector : Selector — SelectedIndex covers FullRow single-select.
                if (element is Selector sel)
                {
                    sel.SelectedIndex = index;
                }
                else
                {
                    throw new ElementActionException(
                        GraftErrorCodes.ActionFailed,
                        $"select is not supported for control type '{element.GetType().Name}'."
                    );
                }

                break;
        }

        element.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static void SelectManyOnUiThread(ElementSelector selector, IReadOnlyList<int> indexes)
    {
        var element = ResolveActionable(selector);
        switch (element)
        {
            case ListBox listBox:
                SelectManyListBox(listBox, selector, indexes);
                break;
            case DataGrid dataGrid:
                SelectManyDataGrid(dataGrid, selector, indexes);
                break;
            default:
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"selectMany is not supported for control type '{element.GetType().Name}' (ListBox or DataGrid)."
                );
        }
    }

    private static void SelectManyListBox(
        ListBox listBox,
        ElementSelector selector,
        IReadOnlyList<int> indexes
    )
    {
        if (listBox.SelectionMode == SelectionMode.Single)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"selectMany requires SelectionMode Multiple or Extended (got Single on '{selector.AutomationId}')."
            );
        }

        ValidateIndexes(indexes, listBox.Items.Count, "List");

        listBox.UnselectAll();

        // Stable order; duplicates collapse via SelectedItems.Add no-op when already selected.
        foreach (var index in indexes.Distinct().OrderBy(static i => i))
        {
            _ = WpfElementScroller.ScrollListItem(listBox, index);
            listBox.SelectedItems.Add(listBox.Items[index]);
        }

        listBox.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static void SelectManyDataGrid(
        DataGrid dataGrid,
        ElementSelector selector,
        IReadOnlyList<int> indexes
    )
    {
        if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"selectMany requires SelectionUnit FullRow (got {dataGrid.SelectionUnit} on '{selector.AutomationId}')."
            );
        }

        if (dataGrid.SelectionMode == DataGridSelectionMode.Single)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"selectMany requires SelectionMode Extended (got Single on '{selector.AutomationId}')."
            );
        }

        ValidateIndexes(indexes, dataGrid.Items.Count, "DataGrid row");

        dataGrid.UnselectAll();

        foreach (var index in indexes.Distinct().OrderBy(static i => i))
        {
            _ = WpfElementScroller.ScrollListItem(dataGrid, index);
            dataGrid.SelectedItems.Add(dataGrid.Items[index]);
        }

        dataGrid.Dispatcher.Invoke(static () => { }, DispatcherPriority.ContextIdle);
    }

    private static void ValidateIndexes(IReadOnlyList<int> indexes, int count, string label)
    {
        foreach (var index in indexes)
        {
            if (index < 0)
            {
                throw new ElementActionException(
                    GraftErrorCodes.SelectorInvalid,
                    "params.indexes entries must be >= 0."
                );
            }

            if (index >= count)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotFound,
                    $"{label} index {index} is out of range (count={count})."
                );
            }
        }
    }

    private static FrameworkElement ResolveActionable(ElementSelector selector)
    {
        var resolver =
            AgentServices.ElementResolver
            ?? throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                "No element resolver is registered. Call WpfGraft.Use() before Agent.Start()."
            );

        var resolved = resolver.Resolve(selector);
        if (resolved.Target is not FrameworkElement element)
        {
            throw new ElementActionException(
                GraftErrorCodes.ActionFailed,
                $"Resolved target is not a FrameworkElement (got {resolved.Target.GetType().Name})."
            );
        }

        if (!element.IsEnabled || !element.IsVisible)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotActionable,
                $"Element '{resolved.AutomationId}' is not actionable (enabled={element.IsEnabled}, visible={element.IsVisible})."
            );
        }

        return element;
    }

    private static void SelectTab(TabControl tab, int index)
    {
        if (index < 0)
        {
            throw new ElementActionException(
                GraftErrorCodes.SelectorInvalid,
                "params.index must be >= 0."
            );
        }

        if (index >= tab.Items.Count)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementNotFound,
                $"Tab index {index} is out of range (count={tab.Items.Count})."
            );
        }

        tab.SelectedIndex = index;
    }
}
