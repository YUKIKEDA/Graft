using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>typeHuman</c>.
/// </summary>
public sealed class TypeHumanParams : ElementTargetParams
{
    /// <summary>
    /// Gets the literal text. JSON null is an empty string. Omitted means the field is missing.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonConverter(typeof(NullAsEmptyStringConverter))]
    public string? Text { get; init; }

    /// <summary>
    /// Gets the delay between Unicode scalars, in milliseconds.
    /// </summary>
    [JsonPropertyName("delayMs")]
    public int? DelayMs { get; init; }
}
