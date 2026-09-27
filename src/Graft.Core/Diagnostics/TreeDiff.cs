using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Diagnostic diff between the last successful tree and the tree captured on failure.
/// </summary>
/// <remarks>
/// Not a JSON Patch document. Identity is a unique <c>automationId</c> when the tree
/// contains that id once; otherwise a path from the root. <c>runtimeId</c> is ignored.
/// </remarks>
public sealed class TreeDiff
{
    /// <summary>
    /// Gets nodes present only in the failure tree.
    /// </summary>
    [JsonPropertyName("added")]
    public required IReadOnlyList<TreeDiffEntry> Added { get; init; }

    /// <summary>
    /// Gets nodes present only in the baseline tree.
    /// </summary>
    [JsonPropertyName("removed")]
    public required IReadOnlyList<TreeDiffEntry> Removed { get; init; }

    /// <summary>
    /// Gets nodes present in both trees whose attributes differ.
    /// </summary>
    [JsonPropertyName("changed")]
    public required IReadOnlyList<TreeDiffChange> Changed { get; init; }
}
