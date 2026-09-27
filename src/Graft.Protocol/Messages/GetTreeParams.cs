using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>getTree</c>.
/// </summary>
public sealed class GetTreeParams
{
    /// <summary>
    /// Gets the maximum visual-tree depth. Negative values are ignored by the agent.
    /// </summary>
    [JsonPropertyName("depth")]
    public int? Depth { get; init; }

    /// <summary>
    /// Gets the maximum node count. Non-positive values are ignored by the agent.
    /// </summary>
    [JsonPropertyName("maxNodes")]
    public int? MaxNodes { get; init; }
}
