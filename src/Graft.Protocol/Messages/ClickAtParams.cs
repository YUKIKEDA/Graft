using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>clickAt</c>.
/// </summary>
public sealed class ClickAtParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the horizontal DIP offset from the clickable point.
    /// </summary>
    [JsonPropertyName("offsetX")]
    public double? OffsetX { get; init; }

    /// <summary>
    /// Gets the vertical DIP offset from the clickable point.
    /// </summary>
    [JsonPropertyName("offsetY")]
    public double? OffsetY { get; init; }
}
