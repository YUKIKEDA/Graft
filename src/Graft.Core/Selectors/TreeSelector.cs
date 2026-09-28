using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core.Selectors;

/// <summary>
/// Resolves a <see cref="Selector"/> against a visual tree via scoring.
/// </summary>
public static class TreeSelector
{
    /// <summary>
    /// Finds the unique best-scoring node at or above the threshold.
    /// </summary>
    /// <param name="root">Tree root (typically getTree root).</param>
    /// <param name="selector">Composite selector.</param>
    /// <returns>The matched node.</returns>
    /// <exception cref="GraftException">
    /// <c>selector.invalid</c>, <c>element.notFound</c>, or <c>element.ambiguous</c>.
    /// </exception>
    public static TreeNode Resolve(TreeNode root, Selector selector)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        try
        {
            return SelectorScoring.Resolve(root, selector.ToQuery());
        }
        catch (SelectorMatchException ex)
        {
            throw new GraftException(ex.Code, ex.Message);
        }
    }

    /// <summary>
    /// Picks a unique child of <paramref name="parent"/> matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="parent">Parent node.</param>
    /// <param name="selector">Child criteria (Name / ControlType / AutomationId).</param>
    /// <param name="nth">Optional zero-based index among matches.</param>
    /// <returns>Matched child.</returns>
    public static TreeNode ResolveChild(TreeNode parent, Selector selector, int? nth = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(selector);
        return ResolveAmong(parent.Children, selector, nth, "child");
    }

    /// <summary>
    /// Picks a unique sibling of <paramref name="node"/> matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="root">Tree root (to locate parent).</param>
    /// <param name="node">Current node.</param>
    /// <param name="selector">Sibling criteria.</param>
    /// <param name="nth">Optional zero-based index among matches.</param>
    /// <returns>Matched sibling.</returns>
    public static TreeNode ResolveSibling(TreeNode root, TreeNode node, Selector selector, int? nth = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(selector);

        if (!TryFindParent(root, node, out var parent) || parent is null)
        {
            throw new GraftException(GraftErrorCodes.ElementNotFound, "Current element has no parent; cannot resolve sibling.");
        }

        var siblings = parent.Children.Where(c => !ReferenceEquals(c, node) && c.RuntimeId != node.RuntimeId).ToList();
        return ResolveAmong(siblings, selector, nth, "sibling");
    }

    /// <summary>
    /// Computes the score for a single node (testing / diagnostics).
    /// </summary>
    /// <param name="node">Candidate node.</param>
    /// <param name="selector">Selector.</param>
    /// <param name="ancestorAutomationIds">Automation ids of ancestors (root → parent).</param>
    /// <returns>Score contribution for this node.</returns>
    public static int Score(TreeNode node, Selector selector, IReadOnlyList<string>? ancestorAutomationIds = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(selector);

        return SelectorScoring.Score(node, selector.ToQuery(), ancestorAutomationIds);
    }

    private static TreeNode ResolveAmong(IReadOnlyList<TreeNode> nodes, Selector selector, int? nth, string label)
    {
        var index = nth ?? selector.Nth;
        var filtered = HasMatchCriterion(selector);
        if (!filtered && index is null)
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, "Selector must specify at least one criterion.");
        }

        var matches = filtered ? nodes.Where(node => Score(node, selector) > 0).ToList() : nodes.ToList();

        if (index is { } i)
        {
            if (i < 0 || i >= matches.Count)
            {
                throw new GraftException(GraftErrorCodes.ElementNotFound, $"No {label} at Nth {i} (count={matches.Count}).");
            }

            return matches[i];
        }

        if (matches.Count == 0)
        {
            throw new GraftException(GraftErrorCodes.ElementNotFound, $"No matching {label} element.");
        }

        if (matches.Count > 1)
        {
            throw new GraftException(GraftErrorCodes.ElementAmbiguous, $"Multiple matching {label} elements ({matches.Count}).");
        }

        return matches[0];
    }

    private static bool HasMatchCriterion(Selector selector) =>
        !string.IsNullOrWhiteSpace(selector.AutomationId)
        || !string.IsNullOrWhiteSpace(selector.Name)
        || !string.IsNullOrWhiteSpace(selector.ControlType)
        || !string.IsNullOrWhiteSpace(selector.NearAutomationId);

    private static bool TryFindParent(TreeNode root, TreeNode target, out TreeNode? parent)
    {
        parent = null;
        foreach (var child in root.Children)
        {
            if (SameNode(child, target))
            {
                parent = root;
                return true;
            }

            if (TryFindParent(child, target, out parent))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SameNode(TreeNode a, TreeNode b) =>
        ReferenceEquals(a, b)
        || (a.RuntimeId != 0 && a.RuntimeId == b.RuntimeId && string.Equals(a.AutomationId, b.AutomationId, StringComparison.Ordinal));
}
