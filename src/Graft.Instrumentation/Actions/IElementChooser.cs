using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>select</c> / <c>selectMany</c> actions.
/// </summary>
public interface IElementChooser
{
    /// <summary>
    /// Selects the item at <paramref name="index"/> on the list/combo matched by
    /// <paramref name="selector"/> (realizes / scrolls as needed).
    /// </summary>
    /// <param name="selector">List or combo selector (automationId required).</param>
    /// <param name="index">Zero-based item index.</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or select failed.</exception>
    void Select(ElementSelector selector, int index);

    /// <summary>
    /// Selects the ListBox / ComboBox / TabControl item whose Automation Name (or display name)
    /// equals <paramref name="key"/> (ordinal). Realizes / scrolls virtualized items as needed.
    /// DataGrid key selection is not supported.
    /// </summary>
    /// <param name="selector">List, combo, or tab selector (automationId required).</param>
    /// <param name="key">Exact Automation Name or display name of the item.</param>
    /// <exception cref="ElementResolveException">
    /// Selector / resolve failures, not found, or ambiguous key.
    /// </exception>
    /// <exception cref="ElementActionException">Not actionable or select failed.</exception>
    void Select(ElementSelector selector, string key);

    /// <summary>
    /// Replaces the multi-selection on a ListBox or DataGrid matched by <paramref name="selector"/>
    /// with the items/rows at <paramref name="indexes"/> (realizes / scrolls as needed).
    /// Empty <paramref name="indexes"/> clears selection.
    /// </summary>
    /// <param name="selector">ListBox or DataGrid selector (automationId required).</param>
    /// <param name="indexes">Zero-based item/row indexes (duplicates ignored).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or selectMany failed.</exception>
    void SelectMany(ElementSelector selector, IReadOnlyList<int> indexes);
}

#endif
