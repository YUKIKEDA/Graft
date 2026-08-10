using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific menu path selection (<c>selectMenu</c>).
/// </summary>
public interface IMenuSelector
{
    /// <summary>
    /// Walks AutomationId segments under the Menu / ContextMenu matched by
    /// <paramref name="selector"/>, opening submenus and activating the leaf.
    /// </summary>
    /// <param name="selector">Menu or open ContextMenu selector (automationId required).</param>
    /// <param name="path">Slash-separated AutomationId path (root not included).</param>
    /// <exception cref="ElementResolveException">Selector / segment not found.</exception>
    /// <exception cref="ElementActionException">Not actionable or selectMenu failed.</exception>
    void SelectMenu(ElementSelector selector, string path);
}

#endif
