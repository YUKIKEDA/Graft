using System.Text.Json.Serialization;
using Graft.Protocol;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for a method that targets one element by the public selector fields.
/// </summary>
public class ElementTargetParams
{
    /// <summary>
    /// Gets or sets the automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; set; }

    /// <summary>
    /// Gets or sets the name criterion.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the control type criterion.
    /// </summary>
    [JsonPropertyName("controlType")]
    public string? ControlType { get; set; }

    /// <summary>
    /// Gets or sets the ancestor automation id used as a near-path score.
    /// </summary>
    [JsonPropertyName("nearAutomationId")]
    public string? NearAutomationId { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index among best-score ties.
    /// </summary>
    [JsonPropertyName("nth")]
    public int? Nth { get; set; }

    /// <summary>
    /// Gets or sets the session-local runtime id. Core does not send this as the wire target.
    /// </summary>
    [JsonPropertyName("runtimeId")]
    public int? RuntimeId { get; set; }

    /// <summary>
    /// Copies these fields into a <see cref="SelectorQuery"/>.
    /// </summary>
    /// <returns>The selector query.</returns>
    public SelectorQuery ToQuery() =>
        new()
        {
            AutomationId = AutomationId,
            Name = Name,
            ControlType = ControlType,
            NearAutomationId = NearAutomationId,
            Nth = Nth,
        };
}
