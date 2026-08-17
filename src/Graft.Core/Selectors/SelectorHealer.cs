using Graft.Core.Diagnostics;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core.Selectors;

/// <summary>
/// Produces alternate selectors when the intended selector fails to resolve (Phase 4).
/// </summary>
public static class SelectorHealer
{
    /// <summary>
    /// Maximum number of candidates attached to a failure report.
    /// </summary>
    public const int MaxCandidates = 5;

    /// <summary>
    /// Reason for a criterion-subset (relaxed) candidate.
    /// </summary>
    public const string ReasonRelaxed = "relaxed";

    /// <summary>
    /// Reason for a Name + ControlType + Near identity candidate.
    /// </summary>
    public const string ReasonStableIdentity = "stableIdentity";

    /// <summary>
    /// Builds ranked healing candidates for <paramref name="failedSelector"/> against <paramref name="root"/>.
    /// </summary>
    /// <param name="root">Tree root.</param>
    /// <param name="failedSelector">Selector that failed to resolve.</param>
    /// <returns>Up to <see cref="MaxCandidates"/> candidates, highest score first.</returns>
    public static IReadOnlyList<HealingCandidate> ProposeCandidates(TreeNode root, Selector failedSelector)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(failedSelector);

        return CollectSuccessful(root, failedSelector)
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Reason, StringComparer.Ordinal)
            .Take(MaxCandidates)
            .Select(c => new HealingCandidate
            {
                Score = c.Score,
                Selector = FailureReportSelector.FromSelector(c.Selector),
                Reason = c.Reason,
            })
            .ToList();
    }

    /// <summary>
    /// Returns a unique high-confidence healed selector when auto-retry is safe.
    /// </summary>
    /// <param name="root">Tree root.</param>
    /// <param name="failedSelector">Selector that failed to resolve.</param>
    /// <param name="healed">Healed selector when the method returns <c>true</c>.</param>
    /// <returns>
    /// <c>true</c> when exactly one best-scoring candidate uniquely resolves a node
    /// that has a non-empty automation id and score ≥ threshold.
    /// </returns>
    public static bool TryGetAutoHeal(TreeNode root, Selector failedSelector, out Selector healed)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(failedSelector);

        healed = null!;
        var successful = CollectSuccessful(root, failedSelector);
        if (successful.Count == 0)
        {
            return false;
        }

        // Prefer relaxed (criterion-subset) candidates for auto-apply so a composite
        // selector with a stale AutomationId can heal uniquely even when the tree
        // has many stable-identity suggestions for the report.
        var relaxed = successful.Where(c => c.Reason == ReasonRelaxed).ToList();
        var pool = relaxed.Count > 0 ? relaxed : successful;

        var bestScore = pool.Max(c => c.Score);
        if (bestScore < SelectorWeights.Threshold)
        {
            return false;
        }

        var top = pool.Where(c => c.Score == bestScore).ToList();
        if (top.Count != 1)
        {
            return false;
        }

        var winner = top[0];
        try
        {
            var node = TreeSelector.Resolve(root, winner.Selector);
            if (string.IsNullOrWhiteSpace(node.AutomationId))
            {
                return false;
            }
        }
        catch (GraftException)
        {
            return false;
        }

        healed = winner.Selector;
        return true;
    }

    private static List<(Selector Selector, int Score, string Reason)> CollectSuccessful(TreeNode root, Selector failedSelector)
    {
        var results = new List<(Selector Selector, int Score, string Reason)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var variant in EnumerateRelaxedVariants(failedSelector))
        {
            if (!TryResolveUnique(root, variant, out var score))
            {
                continue;
            }

            if (!seen.Add(SelectorKey(variant)))
            {
                continue;
            }

            results.Add((variant, score, ReasonRelaxed));
        }

        foreach (var (selector, score) in EnumerateStableIdentityCandidates(root))
        {
            if (SelectorEquals(selector, failedSelector))
            {
                continue;
            }

            if (!seen.Add(SelectorKey(selector)))
            {
                continue;
            }

            results.Add((selector, score, ReasonStableIdentity));
        }

        return results;
    }

    private static IEnumerable<Selector> EnumerateRelaxedVariants(Selector original)
    {
        var slots = new List<(string Kind, string Value)>();
        AddSlot(slots, "automationId", original.AutomationId);
        AddSlot(slots, "name", original.Name);
        AddSlot(slots, "controlType", original.ControlType);
        AddSlot(slots, "near", original.NearAutomationId);

        if (slots.Count == 0)
        {
            yield break;
        }

        var fullMask = (1 << slots.Count) - 1;
        for (var mask = 1; mask <= fullMask; mask++)
        {
            if (mask == fullMask)
            {
                continue;
            }

            string? automationId = null;
            string? name = null;
            string? controlType = null;
            string? near = null;
            for (var i = 0; i < slots.Count; i++)
            {
                if ((mask & (1 << i)) == 0)
                {
                    continue;
                }

                switch (slots[i].Kind)
                {
                    case "automationId":
                        automationId = slots[i].Value;
                        break;
                    case "name":
                        name = slots[i].Value;
                        break;
                    case "controlType":
                        controlType = slots[i].Value;
                        break;
                    case "near":
                        near = slots[i].Value;
                        break;
                }
            }

            yield return new Selector
            {
                AutomationId = automationId,
                Name = name,
                ControlType = controlType,
                NearAutomationId = near,
            };
        }
    }

    private static void AddSlot(List<(string Kind, string Value)> slots, string kind, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            slots.Add((kind, value));
        }
    }

    private static IEnumerable<(Selector Selector, int Score)> EnumerateStableIdentityCandidates(TreeNode root)
    {
        foreach (var (node, ancestors) in WalkWithAncestors(root, []))
        {
            if (!node.Enabled || !node.Visible || string.IsNullOrWhiteSpace(node.AutomationId))
            {
                continue;
            }

            var name = NullIfWhiteSpace(node.Name);
            var controlType = NullIfWhiteSpace(node.ControlType);
            var near = ancestors.LastOrDefault(id => !string.IsNullOrWhiteSpace(id));
            if (name is null || controlType is null || near is null)
            {
                continue;
            }

            var selector = new Selector
            {
                Name = name,
                ControlType = controlType,
                NearAutomationId = near,
            };

            if (!TryResolveUnique(root, selector, out var score))
            {
                continue;
            }

            try
            {
                var resolved = TreeSelector.Resolve(root, selector);
                if (!string.Equals(resolved.AutomationId, node.AutomationId, StringComparison.Ordinal))
                {
                    continue;
                }
            }
            catch (GraftException)
            {
                continue;
            }

            yield return (selector, score);
        }
    }

    private static bool TryResolveUnique(TreeNode root, Selector selector, out int score)
    {
        score = 0;
        if (!selector.HasAnyCriterion())
        {
            return false;
        }

        try
        {
            var node = TreeSelector.Resolve(root, selector);
            var ancestors = FindAncestors(root, node);
            score = TreeSelector.Score(node, selector, ancestors);
            return score >= SelectorWeights.Threshold && !string.IsNullOrWhiteSpace(node.AutomationId);
        }
        catch (GraftException)
        {
            return false;
        }
    }

    private static IEnumerable<(TreeNode Node, IReadOnlyList<string> Ancestors)> WalkWithAncestors(TreeNode node, List<string> ancestors)
    {
        yield return (node, ancestors);
        var next = new List<string>(ancestors) { node.AutomationId };
        foreach (var child in node.Children)
        {
            foreach (var entry in WalkWithAncestors(child, next))
            {
                yield return entry;
            }
        }
    }

    private static IReadOnlyList<string>? FindAncestors(TreeNode root, TreeNode target)
    {
        List<string>? found = null;
        void Walk(TreeNode node, List<string> ancestors)
        {
            if (found is not null)
            {
                return;
            }

            if (ReferenceEquals(node, target) || NodesEqual(node, target))
            {
                found = ancestors;
                return;
            }

            var next = new List<string>(ancestors) { node.AutomationId };
            foreach (var child in node.Children)
            {
                Walk(child, next);
            }
        }

        Walk(root, []);
        return found;
    }

    private static bool NodesEqual(TreeNode a, TreeNode b) =>
        a.RuntimeId == b.RuntimeId && string.Equals(a.AutomationId, b.AutomationId, StringComparison.Ordinal);

    private static string SelectorKey(Selector s) =>
        string.Join(
            '|',
            NullIfWhiteSpace(s.AutomationId) ?? string.Empty,
            NullIfWhiteSpace(s.Name) ?? string.Empty,
            NullIfWhiteSpace(s.ControlType) ?? string.Empty,
            NullIfWhiteSpace(s.NearAutomationId) ?? string.Empty
        );

    private static bool SelectorEquals(Selector a, Selector b) =>
        string.Equals(NullIfWhiteSpace(a.AutomationId), NullIfWhiteSpace(b.AutomationId), StringComparison.Ordinal)
        && string.Equals(NullIfWhiteSpace(a.Name), NullIfWhiteSpace(b.Name), StringComparison.Ordinal)
        && string.Equals(NullIfWhiteSpace(a.ControlType), NullIfWhiteSpace(b.ControlType), StringComparison.Ordinal)
        && string.Equals(NullIfWhiteSpace(a.NearAutomationId), NullIfWhiteSpace(b.NearAutomationId), StringComparison.Ordinal);

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
