using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Wire response envelope: <c>{ v, id, ok, result|error }</c>.
/// </summary>
public sealed class ResponseMessage
{
    /// <summary>
    /// Gets the protocol version (<see cref="ProtocolVersion.Current"/>).
    /// </summary>
    [JsonPropertyName("v")]
    public int V { get; init; }

    /// <summary>
    /// Gets the correlation id matching the request.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets a value indicating whether the call succeeded.
    /// </summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    /// <summary>
    /// Gets the optional success payload.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Result { get; init; }

    /// <summary>
    /// Gets the optional error payload when <see cref="Ok"/> is <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorObject? Error { get; init; }
}
