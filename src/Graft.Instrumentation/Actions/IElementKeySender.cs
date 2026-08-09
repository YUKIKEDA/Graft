using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>sendKeys</c> action (literal Unicode text).
/// </summary>
public interface IElementKeySender
{
    /// <summary>
    /// Focuses the element and types <paramref name="text"/>.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <param name="text">Literal text (no chord DSL).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or sendKeys failed.</exception>
    void SendKeys(ElementSelector selector, string text);
}

#endif
