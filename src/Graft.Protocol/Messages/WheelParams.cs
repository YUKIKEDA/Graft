using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>wheel</c>.
/// </summary>
public sealed class WheelParams : ElementTargetParams
{
    /// <summary>
    /// Gets the wheel delta.
    /// </summary>
    [JsonPropertyName("delta")]
    public int? Delta { get; init; }
}
