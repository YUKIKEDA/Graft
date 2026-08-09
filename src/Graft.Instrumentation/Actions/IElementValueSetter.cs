using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>setValue</c> action (e.g. TextBox replace).
/// </summary>
public interface IElementValueSetter
{
    /// <summary>
    /// Replaces the value of the element matched by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <param name="value">Replacement text (empty string clears).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or setValue failed.</exception>
    void SetValue(ElementSelector selector, string value);
}

#endif
