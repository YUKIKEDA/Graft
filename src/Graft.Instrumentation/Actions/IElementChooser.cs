using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>select</c> action (single selection by index).
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
}

#endif
