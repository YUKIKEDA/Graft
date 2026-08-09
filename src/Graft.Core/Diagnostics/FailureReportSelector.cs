using System.Text.Json.Serialization;
using Graft.Core.Selectors;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Selector criteria snapshot embedded in a <see cref="FailureReport"/>.
/// </summary>
public sealed class FailureReportSelector
{
    /// <summary>
    /// Gets the automation id criterion when set.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the name criterion when set.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the control type criterion when set.
    /// </summary>
    [JsonPropertyName("controlType")]
    public string? ControlType { get; init; }

    /// <summary>
    /// Gets the near-path automation id criterion when set.
    /// </summary>
    [JsonPropertyName("nearAutomationId")]
    public string? NearAutomationId { get; init; }

    /// <summary>
    /// Creates a report selector from a live <see cref="Selector"/>.
    /// </summary>
    /// <param name="selector">Source selector.</param>
    /// <returns>A serializable snapshot.</returns>
    public static FailureReportSelector FromSelector(Selector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new FailureReportSelector
        {
            AutomationId = selector.AutomationId,
            Name = selector.Name,
            ControlType = selector.ControlType,
            NearAutomationId = selector.NearAutomationId,
        };
    }
}
