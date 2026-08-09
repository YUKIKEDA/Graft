using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Wire error payload: <c>{ code, message, details? }</c>.
/// </summary>
public sealed class ErrorObject
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Details { get; init; }
}
