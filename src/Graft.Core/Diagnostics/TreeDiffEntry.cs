using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// One added or removed node in a <see cref="TreeDiff"/>.
/// </summary>
public sealed class TreeDiffEntry
{
    /// <summary>
    /// Gets the stable path or unique automation id for this node.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// Gets the node snapshot without children.
    /// </summary>
    [JsonPropertyName("node")]
    public required TreeDiffNode Node { get; init; }
}
