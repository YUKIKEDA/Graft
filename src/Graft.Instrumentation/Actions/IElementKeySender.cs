using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific keyboard actions (<c>sendKeys</c>, <c>typeHuman</c>, <c>pressKeys</c>).
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

    /// <summary>
    /// Focuses the element and types <paramref name="text"/> one Unicode scalar at a time.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <param name="text">Literal text (no chord DSL). Existing text is left in place.</param>
    /// <param name="delay">Wait between scalars. Zero types without an extra pause. Must not be negative.</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable, negative delay, or typeHuman failed.</exception>
    /// <remarks>
    /// The wait runs on the caller thread, which is the agent request thread, so the UI dispatcher can
    /// process debounce between characters.
    /// </remarks>
    void TypeHuman(ElementSelector selector, string text, TimeSpan delay);

    /// <summary>
    /// Focuses the element and presses one keyboard chord.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <param name="keys">Chord DSL (e.g. <c>Control+A</c>).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable, invalid chord, or press failed.</exception>
    void PressKeys(ElementSelector selector, string keys);
}

#endif
