using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>pressKeys</c>.
/// </summary>
public sealed class PressKeysParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the chord DSL string.
    /// </summary>
    [JsonPropertyName("keys")]
    public string? Keys { get; init; }
}
