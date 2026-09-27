using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>sendKeys</c>.
/// </summary>
public sealed class SendKeysParams : ElementTargetParams
{
    /// <summary>
    /// Gets the literal text. JSON null is an empty string. Omitted means the field is missing.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonConverter(typeof(NullAsEmptyStringConverter))]
    public string? Text { get; init; }
}
