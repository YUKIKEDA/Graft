using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Success payload for <c>getTree</c>: <c>{ root, truncated }</c>.
/// </summary>
public sealed class GetTreeResult
{
    /// <summary>
    /// Gets the root tree node (typically the main window).
    /// </summary>
    [JsonPropertyName("root")]
    public required TreeNode Root { get; init; }

    /// <summary>
    /// Gets a value indicating whether depth or node limits truncated the tree.
    /// </summary>
    [JsonPropertyName("truncated")]
    public bool Truncated { get; init; }
}
