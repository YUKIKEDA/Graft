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

        if (
            string.IsNullOrWhiteSpace(selector.AutomationId)
            && string.IsNullOrWhiteSpace(selector.Name)
            && string.IsNullOrWhiteSpace(selector.ControlType)
            && string.IsNullOrWhiteSpace(selector.NearAutomationId)
        )
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, "Selector must specify at least one criterion.");
        }

        if (selector.Nth is < 0)
        {
            throw new GraftException(GraftErrorCodes.SelectorInvalid, "Selector.Nth must be >= 0 when specified.");
        }

        var candidates = new List<(TreeNode Node, int Score)>();
        Walk(root, ancestors: [], selector, candidates);

        var qualifying = candidates.Where(c => c.Score >= SelectorWeights.Threshold).OrderByDescending(c => c.Score).ToList();

        if (qualifying.Count == 0)
        {
            throw new GraftException(GraftErrorCodes.ElementNotFound, "No element scored at or above the selector threshold.");
        }

        var bestScore = qualifying[0].Score;
        var tied = qualifying.Where(c => c.Score == bestScore).ToList();

        if (selector.Nth is { } nth)
        {
            // Tree order among best-score ties (DFS discovery order preserved in Walk).
            if (nth >= tied.Count)
            {
                throw new GraftException(GraftErrorCodes.ElementNotFound, $"Selector.Nth {nth} is out of range (count={tied.Count}).");
            }

            return tied[nth].Node;
        }

        if (tied.Count > 1)
        {
            throw new GraftException(GraftErrorCodes.ElementAmbiguous, $"Multiple elements tied for best selector score ({bestScore}).");
        }

        return tied[0].Node;
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

        // Hard gates: AutomationId / Name / ControlType fail closed when specified (Phase 27 F02).
        if (!string.IsNullOrWhiteSpace(selector.AutomationId))
        {
            if (!string.Equals(node.AutomationId, selector.AutomationId, StringComparison.Ordinal))
            {
                return 0;
            }
        }

        if (!string.IsNullOrWhiteSpace(selector.Name))
        {
            if (!string.Equals(node.Name, selector.Name, StringComparison.Ordinal))
            {
                return 0;
            }
        }

        if (!string.IsNullOrWhiteSpace(selector.ControlType))
        {
            if (!string.Equals(node.ControlType, selector.ControlType, StringComparison.Ordinal))
            {
                return 0;
            }
        }

        var score = 0;
        if (!string.IsNullOrWhiteSpace(selector.AutomationId))
        {
            score += SelectorWeights.AutomationId;
        }

        if (!string.IsNullOrWhiteSpace(selector.Name))
        {
            score += SelectorWeights.Name;
        }

        if (!string.IsNullOrWhiteSpace(selector.ControlType))
        {
            score += SelectorWeights.ControlType;
        }

        if (
            !string.IsNullOrWhiteSpace(selector.NearAutomationId)
            && ancestorAutomationIds is not null
            && ancestorAutomationIds.Any(id => string.Equals(id, selector.NearAutomationId, StringComparison.Ordinal))
        )
        {
            score += SelectorWeights.NearPath;
        }

        return score;
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

    private static void Walk(TreeNode node, List<string> ancestors, Selector selector, List<(TreeNode Node, int Score)> candidates)
    {
        var score = Score(node, selector, ancestors);
        if (score > 0)
        {
            candidates.Add((node, score));
        }

        var nextAncestors = new List<string>(ancestors) { node.AutomationId };
        foreach (var child in node.Children)
        {
            Walk(child, nextAncestors, selector, candidates);
        }
    }
}
