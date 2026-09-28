using Graft.Protocol.Messages;

namespace Graft.Protocol;

/// <summary>
/// Scores a <see cref="SelectorQuery"/> against a <see cref="TreeNode"/> tree.
/// </summary>
public static class SelectorScoring
{
    /// <summary>
    /// Finds the unique best-scoring node at or above the threshold.
    /// </summary>
    /// <param name="root">Tree root.</param>
    /// <param name="query">Selector fields.</param>
    /// <returns>The matched node.</returns>
    /// <exception cref="SelectorMatchException">The query is invalid, nothing matches, or the best score is tied.</exception>
    public static TreeNode Resolve(TreeNode root, SelectorQuery query)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(query);
        var tied = BestTies(root, query);
        if (query.Nth is { } nth)
        {
            if (nth >= tied.Count)
            {
                throw new SelectorMatchException(GraftErrorCodes.ElementNotFound, $"Selector.Nth {nth} is out of range (count={tied.Count}).");
            }

            return tied[nth].Node;
        }

        if (tied.Count > 1)
        {
            throw new SelectorMatchException(GraftErrorCodes.ElementAmbiguous, $"Multiple elements tied for best selector score ({tied[0].Score}).");
        }

        return tied[0].Node;
    }

    /// <summary>
    /// Returns where <paramref name="node"/> sits among the best-score ties, in tree order.
    /// </summary>
    /// <param name="root">Tree root.</param>
    /// <param name="node">Node to locate.</param>
    /// <param name="query">Selector fields. <see cref="SelectorQuery.Nth"/> is ignored.</param>
    /// <returns>The zero-based index and the tie count. Index is -1 when the node is not in the tie.</returns>
    public static (int Index, int Count) Place(TreeNode root, TreeNode node, SelectorQuery query)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(query);
        var withoutNth = new SelectorQuery
        {
            AutomationId = query.AutomationId,
            Name = query.Name,
            ControlType = query.ControlType,
            NearAutomationId = query.NearAutomationId,
        };
        var tied = BestTies(root, withoutNth);
        for (var i = 0; i < tied.Count; i++)
        {
            if (SameNode(tied[i].Node, node))
            {
                return (i, tied.Count);
            }
        }

        return (-1, tied.Count);
    }

    /// <summary>
    /// Computes the score for a single node.
    /// </summary>
    /// <param name="node">Candidate node.</param>
    /// <param name="query">Selector fields.</param>
    /// <param name="ancestorAutomationIds">Automation ids of ancestors (root to parent).</param>
    /// <returns>Score contribution for this node. Zero when a hard gate misses.</returns>
    public static int Score(TreeNode node, SelectorQuery query, IReadOnlyList<string>? ancestorAutomationIds = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(query);

        if (!string.IsNullOrWhiteSpace(query.AutomationId) && !string.Equals(node.AutomationId, query.AutomationId, StringComparison.Ordinal))
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(query.Name) && !string.Equals(node.Name, query.Name, StringComparison.Ordinal))
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(query.ControlType) && !string.Equals(node.ControlType, query.ControlType, StringComparison.Ordinal))
        {
            return 0;
        }

        var score = 0;
        if (!string.IsNullOrWhiteSpace(query.AutomationId))
        {
            score += SelectorWeights.AutomationId;
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            score += SelectorWeights.Name;
        }

        if (!string.IsNullOrWhiteSpace(query.ControlType))
        {
            score += SelectorWeights.ControlType;
        }

        if (
            !string.IsNullOrWhiteSpace(query.NearAutomationId)
            && ancestorAutomationIds is not null
            && ancestorAutomationIds.Any(id => string.Equals(id, query.NearAutomationId, StringComparison.Ordinal))
        )
        {
            score += SelectorWeights.NearPath;
        }

        return score;
    }

    private static List<(TreeNode Node, int Score)> BestTies(TreeNode root, SelectorQuery query)
    {
        if (!query.HasCriterion)
        {
            throw new SelectorMatchException(GraftErrorCodes.SelectorInvalid, "Selector must specify at least one criterion.");
        }

        if (query.Nth is < 0)
        {
            throw new SelectorMatchException(GraftErrorCodes.SelectorInvalid, "Selector.Nth must be >= 0 when specified.");
        }

        var candidates = new List<(TreeNode Node, int Score)>();
        Walk(root, [], query, candidates);
        var qualifying = candidates.Where(c => c.Score >= SelectorWeights.Threshold).OrderByDescending(c => c.Score).ToList();
        if (qualifying.Count == 0)
        {
            throw new SelectorMatchException(GraftErrorCodes.ElementNotFound, "No element scored at or above the selector threshold.");
        }

        var bestScore = qualifying[0].Score;
        return qualifying.Where(c => c.Score == bestScore).ToList();
    }

    private static void Walk(TreeNode node, List<string> ancestors, SelectorQuery query, List<(TreeNode Node, int Score)> candidates)
    {
        var score = Score(node, query, ancestors);
        if (score > 0)
        {
            candidates.Add((node, score));
        }

        var nextAncestors = new List<string>(ancestors) { node.AutomationId };
        foreach (var child in node.Children)
        {
            Walk(child, nextAncestors, query, candidates);
        }
    }

    private static bool SameNode(TreeNode a, TreeNode b) =>
        ReferenceEquals(a, b)
        || (a.RuntimeId != 0 && a.RuntimeId == b.RuntimeId && string.Equals(a.AutomationId, b.AutomationId, StringComparison.Ordinal));
}
