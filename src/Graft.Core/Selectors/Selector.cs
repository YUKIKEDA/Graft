namespace Graft.Core.Selectors;

/// <summary>
/// Composite selector scored against a <see cref="Protocol.Messages.TreeNode"/> tree.
/// </summary>
public sealed class Selector
{
    /// <summary>
    /// Gets the automation id criterion (exact match).
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the name criterion (exact match).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the control type criterion (exact match).
    /// </summary>
    public string? ControlType { get; init; }

    /// <summary>
    /// Gets a near-path stub: score when an ancestor has this automation id.
    /// </summary>
    public string? NearAutomationId { get; init; }

    /// <summary>
    /// Creates a shorthand selector that matches <paramref name="automationId"/> only.
    /// </summary>
    /// <param name="automationId">Automation id to match.</param>
    /// <returns>A selector with score weight 100 when matched.</returns>
    public static Selector ByAutomationId(string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return new Selector { AutomationId = automationId };
    }

    internal bool HasAnyCriterion() =>
        !string.IsNullOrWhiteSpace(AutomationId)
        || !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrWhiteSpace(ControlType)
        || !string.IsNullOrWhiteSpace(NearAutomationId);
}
