using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific TreeView path selection (<c>selectTree</c>).
/// </summary>
public interface ITreeSelector
{
    /// <summary>
    /// Walks AutomationId segments under the TreeView matched by <paramref name="selector"/>,
    /// expanding intermediates and selecting the leaf (<c>IsSelected = true</c>).
    /// </summary>
    /// <param name="selector">TreeView root selector (automationId required).</param>
    /// <param name="path">Slash-separated AutomationId path (root not included).</param>
    /// <exception cref="ElementResolveException">Selector / segment not found.</exception>
    /// <exception cref="ElementActionException">Not actionable or selectTree failed.</exception>
    void SelectTree(ElementSelector selector, string path);
}

#endif
