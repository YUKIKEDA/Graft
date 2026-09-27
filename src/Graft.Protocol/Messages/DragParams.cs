using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>drag</c>.
/// </summary>
public sealed class DragParams
{
    /// <summary>
    /// Gets the source automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the drop-target automation id.
    /// </summary>
    [JsonPropertyName("toAutomationId")]
    public string? ToAutomationId { get; init; }
}
