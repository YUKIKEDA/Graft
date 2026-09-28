using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>pressKeys</c>.
/// </summary>
public sealed class PressKeysParams : ElementTargetParams
{
    /// <summary>
    /// Gets the chord DSL string.
    /// </summary>
    [JsonPropertyName("keys")]
    public string? Keys { get; init; }
}
