using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Wire request envelope: <c>{ v, id, method, params }</c>.
/// </summary>
public sealed class RequestMessage
{
    /// <summary>
    /// Gets the protocol version (<see cref="ProtocolVersion.Current"/>).
    /// </summary>
    [JsonPropertyName("v")]
    public int V { get; init; }

    /// <summary>
    /// Gets the correlation id for matching responses.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the method name to invoke on the agent.
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    /// Gets the optional JSON parameters object.
    /// </summary>
    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Params { get; init; }
}
