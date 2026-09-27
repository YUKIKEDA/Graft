using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>scrollIntoView</c>.
/// </summary>
public sealed class ScrollIntoViewParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the optional list item index.
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; init; }
}
