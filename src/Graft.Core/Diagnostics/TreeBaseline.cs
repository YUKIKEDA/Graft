using Graft.Protocol.Messages;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Remembers the last tree that backed a successful operation in this session.
/// </summary>
internal sealed class TreeBaseline
{
    private TreeNode? _last;

    /// <summary>
    /// Gets or sets a value indicating whether successes are stored and diffs are produced.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Stores <paramref name="root"/> as the next failure's baseline.
    /// </summary>
    /// <param name="root">Tree root from the successful operation.</param>
    public void Remember(TreeNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!Enabled)
        {
            return;
        }

        _last = root;
    }

    /// <summary>
    /// Drops the baseline. Used when the target window changes.
    /// </summary>
    public void Clear() => _last = null;

    /// <summary>
    /// Diffs <paramref name="current"/> against the baseline.
    /// </summary>
    /// <param name="current">Tree root captured on failure.</param>
    /// <returns>The diff, or <see langword="null"/> when disabled or no success has been stored.</returns>
    public TreeDiff? Diff(TreeNode current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (!Enabled || _last is null)
        {
            return null;
        }

        return TreeDiffer.Diff(_last, current);
    }
}
