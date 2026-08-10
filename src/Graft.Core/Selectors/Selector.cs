namespace Graft.Core.Selectors;

/// <summary>
/// Composite selector scored against a <see cref="Protocol.Messages.TreeNode"/> tree.
/// </summary>
public sealed class Selector
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
    /// Gets an optional zero-based index among qualifying matches in tree order.
    /// </summary>
    public int? Nth { get; init; }

    /// <summary>
    /// Creates a shorthand selector that matches <paramref name="automationId"/> only.
    /// </summary>
    /// <param name="automationId">Automation id to match.</param>
    /// <returns>A selector with hard automation id match.</returns>
    public static Selector ByAutomationId(string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return new Selector { AutomationId = automationId };
    }

    /// <summary>
    /// Creates a shorthand selector that matches <paramref name="name"/> only.
    /// </summary>
    /// <param name="name">Automation / display name to match.</param>
    /// <returns>A selector with hard name match.</returns>
    public static Selector ByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Selector { Name = name };
    }

    /// <summary>
    /// Creates a shorthand selector that matches <paramref name="controlType"/> only.
    /// </summary>
    /// <param name="controlType">Control type label (e.g. <c>Button</c>).</param>
    /// <returns>A selector with hard control type match.</returns>
    public static Selector ByControlType(string controlType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controlType);
        return new Selector { ControlType = controlType };
    }

    internal bool HasAnyCriterion() =>
        !string.IsNullOrWhiteSpace(AutomationId)
        || !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrWhiteSpace(ControlType)
        || !string.IsNullOrWhiteSpace(NearAutomationId)
        || Nth is not null;
}
