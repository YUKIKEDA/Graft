using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>expand</c> / <c>collapse</c> actions.
/// </summary>
public interface IElementExpander
{
    /// <summary>
    /// Expands the element matched by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or expand failed.</exception>
    void Expand(ElementSelector selector);

    /// <summary>
    /// Collapses the element matched by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or collapse failed.</exception>
    void Collapse(ElementSelector selector);
}

#endif
