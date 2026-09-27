using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>setValue</c>.
/// </summary>
public sealed class SetValueParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the replacement text. JSON null is an empty string. Omitted means the field is missing.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonConverter(typeof(NullAsEmptyStringConverter))]
    public string? Value { get; init; }
}
