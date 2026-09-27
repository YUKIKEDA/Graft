using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// An attribute change for a node that exists in both trees.
/// </summary>
public sealed class TreeDiffChange
{
    /// <summary>
    /// Gets the stable path or unique automation id for this node.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// Gets the names of attributes that differ, in a fixed order.
    /// </summary>
    [JsonPropertyName("fields")]
    public required IReadOnlyList<string> Fields { get; init; }

    /// <summary>
    /// Gets the baseline snapshot.
    /// </summary>
    [JsonPropertyName("before")]
    public required TreeDiffNode Before { get; init; }

    /// <summary>
    /// Gets the failure-time snapshot.
    /// </summary>
    [JsonPropertyName("after")]
    public required TreeDiffNode After { get; init; }
}
