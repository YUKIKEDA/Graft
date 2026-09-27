using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>select</c>. Exactly one of <see cref="Index"/> or <see cref="Key"/> is set.
/// </summary>
public sealed class SelectParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the item index when selecting by position.
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; init; }

    /// <summary>
    /// Gets the item key when selecting by display name.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}
