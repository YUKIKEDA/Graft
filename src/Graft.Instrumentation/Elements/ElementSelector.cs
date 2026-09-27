using Graft.Protocol;

namespace Graft.Instrumentation.Elements;

#if GRAFT_TEST

/// <summary>
/// Selector for resolving a live element from the public selector fields.
/// </summary>
public sealed class ElementSelector
{
    /// <summary>
    /// Gets the automation id to match.
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the name criterion.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the control type criterion.
    /// </summary>
    public string? ControlType { get; init; }

    /// <summary>
    /// Gets the ancestor automation id used as a near-path score.
    /// </summary>
    public string? NearAutomationId { get; init; }

    /// <summary>
    /// Gets the zero-based index among best-score ties.
    /// </summary>
    public int? Nth { get; init; }

    /// <summary>
    /// Gets an optional runtime id from a prior <c>getTree</c> capture in the same walk order.
    /// </summary>
    /// <remarks>
    /// Core does not send this as the wire target. Screenshot still accepts it when no selector field is set.
    /// </remarks>
    public int? RuntimeId { get; init; }

    /// <summary>
    /// Gets a value indicating whether a scored selector field is set.
    /// </summary>
    public bool HasScoringField =>
        !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrWhiteSpace(ControlType)
        || !string.IsNullOrWhiteSpace(NearAutomationId)
        || Nth is not null;

    /// <summary>
    /// Copies <paramref name="query"/> into an element selector.
    /// </summary>
    /// <param name="query">Wire selector fields.</param>
    /// <returns>The element selector.</returns>
    public static ElementSelector FromQuery(SelectorQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new ElementSelector
        {
            AutomationId = query.AutomationId,
            Name = query.Name,
            ControlType = query.ControlType,
            NearAutomationId = query.NearAutomationId,
            Nth = query.Nth,
        };
    }
}

#endif
