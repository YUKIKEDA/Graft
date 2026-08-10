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
}

#endif
