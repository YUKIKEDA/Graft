using Graft.Instrumentation.Elements;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>scrollIntoView</c> action.
/// </summary>
public interface IElementScroller
{
    /// <summary>
    /// Scrolls the target into view and returns the realized element identity.
    /// </summary>
    /// <param name="selector">Element or list selector (automationId required).</param>
    /// <param name="index">
    /// When set, <paramref name="selector"/> is treated as a list/combo and the item at
    /// this index is realized and scrolled into view.
    /// </param>
    /// <returns>Identity of the scrolled element (item when <paramref name="index"/> is set).</returns>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Scroll / realize failed.</exception>
    ElementIdentity ScrollIntoView(ElementSelector selector, int? index);
}

#endif
