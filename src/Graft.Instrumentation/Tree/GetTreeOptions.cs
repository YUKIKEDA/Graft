namespace Graft.Instrumentation.Tree;

#if GRAFT_TEST

/// <summary>
/// Options for a <c>getTree</c> capture.
/// </summary>
public sealed class GetTreeOptions
{
    /// <summary>
    /// Default maximum tree depth (root is depth 0).
    /// </summary>
    public const int DefaultMaxDepth = 25;

    /// <summary>
    /// Default maximum node count including the root.
    /// </summary>
    public const int DefaultMaxNodes = 2000;

    /// <summary>
    /// Gets the maximum depth to walk (root is depth 0).
    /// </summary>
    public int MaxDepth { get; init; } = DefaultMaxDepth;

    /// <summary>
    /// Gets the maximum number of nodes to include.
    /// </summary>
    public int MaxNodes { get; init; } = DefaultMaxNodes;
}

#endif
