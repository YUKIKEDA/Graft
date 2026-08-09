namespace Graft.Instrumentation.Elements;

#if GRAFT_TEST

/// <summary>
/// Selector for resolving a live UI element (Phase 1: automationId-first).
/// </summary>
public sealed class ElementSelector
{
    /// <summary>
    /// Gets the automation id to match (required for Phase 1).
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets an optional runtime id from a prior <c>getTree</c> capture in the same walk order.
    /// </summary>
    public int? RuntimeId { get; init; }
}

#endif
