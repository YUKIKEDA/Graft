using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>wheel</c>.
/// </summary>
public sealed class WheelParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the wheel delta.
    /// </summary>
    [JsonPropertyName("delta")]
    public int? Delta { get; init; }
}
