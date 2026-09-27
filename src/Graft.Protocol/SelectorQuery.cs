namespace Graft.Protocol;

/// <summary>
/// Public selector fields carried on an element action.
/// </summary>
public sealed class SelectorQuery
{
    /// <summary>
    /// Gets the automation id criterion (exact match; hard when set).
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the name criterion (exact match; hard when set).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the control type criterion (exact match; hard when set).
    /// </summary>
    public string? ControlType { get; init; }

    /// <summary>
    /// Gets a near-path stub: score when an ancestor has this automation id.
    /// </summary>
    public string? NearAutomationId { get; init; }

    /// <summary>
    /// Gets an optional zero-based index among best-score ties in tree order.
    /// </summary>
    public int? Nth { get; init; }

    /// <summary>
    /// Gets a value indicating whether at least one match field is set.
    /// </summary>
    public bool HasCriterion =>
        !string.IsNullOrWhiteSpace(AutomationId)
        || !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrWhiteSpace(ControlType)
        || !string.IsNullOrWhiteSpace(NearAutomationId);
}
