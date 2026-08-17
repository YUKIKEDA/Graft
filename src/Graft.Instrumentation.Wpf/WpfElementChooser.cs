using System.Windows;
using System.Windows.Automation;
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
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot select.");
        }

        if (dispatcher.CheckAccess())
        {
            SelectOnUiThread(selector, index);
            return;
        }

        dispatcher.Invoke(() => SelectOnUiThread(selector, index), DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void Select(ElementSelector selector, string key)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot select.");
        }

        if (dispatcher.CheckAccess())
        {
            SelectByKeyOnUiThread(selector, key);
            return;
        }

        dispatcher.Invoke(() => SelectByKeyOnUiThread(selector, key), DispatcherPriority.Normal);
    }

    /// <inheritdoc />
    public void SelectMany(ElementSelector selector, IReadOnlyList<int> indexes)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(indexes);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "WPF Application.Current is not available; cannot selectMany.");
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

    private static void SelectByKeyOnUiThread(ElementSelector selector, string key)
    {
        var element = ResolveActionable(selector);

        switch (element)
        {
            case TabControl tab:
                SelectTabByKey(tab, key);
                break;
            case DataGrid:
                throw new ElementActionException(GraftErrorCodes.ActionFailed, "select by key is not supported for DataGrid.");
            case Selector sel:
                SelectSelectorByKey(sel, key);
                break;
            default:
                throw new ElementActionException(
                    GraftErrorCodes.ActionFailed,
                    $"select by key is not supported for control type '{element.GetType().Name}'."
                );
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

    private static void SelectManyListBox(ListBox listBox, ElementSelector selector, IReadOnlyList<int> indexes)
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

    private static void SelectManyDataGrid(DataGrid dataGrid, ElementSelector selector, IReadOnlyList<int> indexes)
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
                throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.indexes entries must be >= 0.");
            }

            if (index >= count)
            {
                throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"{label} index {index} is out of range (count={count}).");
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
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.index must be >= 0.");
        }

        if (index >= tab.Items.Count)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"Tab index {index} is out of range (count={tab.Items.Count}).");
        }

        tab.SelectedIndex = index;
    }

    private static void SelectTabByKey(TabControl tab, string key)
    {
        int? matchIndex = null;

        for (var i = 0; i < tab.Items.Count; i++)
        {
            var item = tab.Items[i];
            var tabItem = item as TabItem ?? tab.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;
            var displayName = ResolveTabItemKey(tabItem, item);
            if (!string.Equals(displayName, key, StringComparison.Ordinal))
            {
                continue;
            }

            if (matchIndex is not null)
            {
                throw new ElementResolveException(GraftErrorCodes.ElementAmbiguous, $"Multiple tab items matched key '{key}'.");
            }

            matchIndex = i;
        }

        if (matchIndex is null)
        {
            throw new ElementResolveException(GraftErrorCodes.ElementNotFound, $"No tab item matched key '{key}'.");
        }

        tab.SelectedIndex = matchIndex.Value;
    }

    private static void SelectSelectorByKey(Selector selector, string key)
    {
        int? matchIndex = null;

        for (var i = 0; i < selector.Items.Count; i++)
        {
            _ = WpfElementScroller.ScrollListItem(selector, i);
            var container = selector.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            var displayName = ResolveItemDisplayName(container, selector.Items[i]);
            if (!string.Equals(displayName, key, StringComparison.Ordinal))
            {
                continue;
            }

            if (matchIndex is not null)
            {
                throw new ElementResolveException(GraftErrorCodes.ElementAmbiguous, $"Multiple items matched key '{key}'.");
            }

            matchIndex = i;
        }

        if (matchIndex is null)
        {
            throw new ElementResolveException(GraftErrorCodes.ElementNotFound, $"No item matched key '{key}'.");
        }

        _ = WpfElementScroller.ScrollListItem(selector, matchIndex.Value);
        selector.SelectedIndex = matchIndex.Value;
    }

    private static string ResolveTabItemKey(TabItem? tabItem, object? item)
    {
        var element = tabItem ?? item as TabItem;
        if (element is not null)
        {
            var automationName = AutomationProperties.GetName(element);
            if (!string.IsNullOrEmpty(automationName))
            {
                return automationName;
            }

            if (element.Header is string header)
            {
                return header;
            }
        }

        return item as string ?? string.Empty;
    }

    private static string ResolveItemDisplayName(FrameworkElement? container, object? item)
    {
        if (container is not null)
        {
            var automationName = AutomationProperties.GetName(container);
            if (!string.IsNullOrEmpty(automationName))
            {
                return automationName;
            }

            switch (container)
            {
                case HeaderedContentControl { Header: string header }:
                    return header;
                case HeaderedItemsControl { Header: string itemsHeader }:
                    return itemsHeader;
                case ContentControl { Content: string content }:
                    return content;
            }
        }

        return item as string ?? string.Empty;
    }
}
