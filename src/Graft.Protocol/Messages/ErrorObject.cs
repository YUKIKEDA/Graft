using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Wire error payload: <c>{ code, message, details? }</c>.
/// </summary>
public sealed class ErrorObject
{
    /// <summary>
    /// Gets the stable error code (see <see cref="GraftErrorCodes"/>).
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error description.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets optional structured details for diagnostics.
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Details { get; init; }
}
