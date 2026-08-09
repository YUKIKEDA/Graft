using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>toggle</c> action (e.g. CheckBox flip).
/// </summary>
public interface IElementToggler
{
    /// <summary>
    /// Toggles the element matched by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or toggle failed.</exception>
    void Toggle(ElementSelector selector);
}

#endif
