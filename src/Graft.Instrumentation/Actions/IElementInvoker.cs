using Graft.Instrumentation.Elements;

namespace Graft.Instrumentation.Actions;

#if GRAFT_TEST

/// <summary>
/// Framework-specific <c>invoke</c> action (e.g. button click).
/// </summary>
public interface IElementInvoker
{
    /// <summary>
    /// Invokes the element matched by <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or invoke failed.</exception>
    void Invoke(ElementSelector selector);

    /// <summary>
    /// Queues an invoke on the UI dispatcher without waiting for completion.
    /// </summary>
    /// <remarks>
    /// Use when the invoke may open a modal (<c>ShowDialog</c>) that would otherwise
    /// block a synchronous <see cref="Invoke"/> until the dialog closes.
    /// </remarks>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures before queueing.</exception>
    /// <exception cref="ElementActionException">Dispatcher unavailable.</exception>
    void BeginInvoke(ElementSelector selector);

    /// <summary>
    /// Right-clicks the element matched by <paramref name="selector"/> (SendInput).
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <exception cref="ElementResolveException">Selector / resolve failures.</exception>
    /// <exception cref="ElementActionException">Not actionable or right-click failed.</exception>
    void RightClick(ElementSelector selector);

    /// <summary>
    /// Double-clicks the element matched by <paramref name="selector"/> (SendInput).
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    void DoubleClick(ElementSelector selector);

    /// <summary>
    /// Moves the cursor over the element matched by <paramref name="selector"/> (SendInput).
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    void Hover(ElementSelector selector);

    /// <summary>
    /// Drags from <paramref name="from"/> to <paramref name="to"/> with the left button (SendInput).
    /// </summary>
    /// <param name="from">Source element selector.</param>
    /// <param name="to">Target element selector.</param>
    void Drag(ElementSelector from, ElementSelector to);

    /// <summary>
    /// Left-clicks at the element's clickable point plus DIP offsets (SendInput).
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <param name="offsetX">Horizontal offset in DIP from the clickable point.</param>
    /// <param name="offsetY">Vertical offset in DIP from the clickable point.</param>
    void ClickAt(ElementSelector selector, double offsetX, double offsetY);

    /// <summary>
    /// Scrolls the mouse wheel over the element matched by <paramref name="selector"/> (SendInput).
    /// </summary>
    /// <param name="selector">Element selector (automationId required).</param>
    /// <param name="delta">Wheel delta (typically multiples of 120).</param>
    void Wheel(ElementSelector selector, int delta);
}

#endif
