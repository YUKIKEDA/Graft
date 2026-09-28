using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>scrollIntoView</c>.
/// </summary>
public sealed class ScrollIntoViewParams : ElementTargetParams
{
    /// <summary>
    /// Gets the optional list item index.
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; init; }
}
