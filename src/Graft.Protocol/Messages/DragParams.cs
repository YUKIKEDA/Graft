using System.Text.Json.Serialization;
using Graft.Protocol;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>drag</c>.
/// </summary>
public sealed class DragParams : ElementTargetParams
{
    /// <summary>
    /// Gets or sets the drop-target automation id.
    /// </summary>
    [JsonPropertyName("toAutomationId")]
    public string? ToAutomationId { get; set; }

    /// <summary>
    /// Gets or sets the drop-target name criterion.
    /// </summary>
    [JsonPropertyName("toName")]
    public string? ToName { get; set; }

    /// <summary>
    /// Gets or sets the drop-target control type criterion.
    /// </summary>
    [JsonPropertyName("toControlType")]
    public string? ToControlType { get; set; }

    /// <summary>
    /// Gets or sets the drop-target near-path automation id.
    /// </summary>
    [JsonPropertyName("toNearAutomationId")]
    public string? ToNearAutomationId { get; set; }

    /// <summary>
    /// Gets or sets the drop-target index among best-score ties.
    /// </summary>
    [JsonPropertyName("toNth")]
    public int? ToNth { get; set; }

    /// <summary>
    /// Copies the drop target into a <see cref="SelectorQuery"/>.
    /// </summary>
    /// <returns>The drop-target query.</returns>
    public SelectorQuery ToTargetQuery() =>
        new()
        {
            AutomationId = ToAutomationId,
            Name = ToName,
            ControlType = ToControlType,
            NearAutomationId = ToNearAutomationId,
            Nth = ToNth,
        };
}
