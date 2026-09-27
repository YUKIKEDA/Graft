using Graft.Protocol.Messages;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Builds a <see cref="TreeDiff"/> from two full trees returned by the agent.
/// </summary>
public static class TreeDiffer
{
    /// <summary>
    /// DIP slack for bounds. Below one logical pixel, so transform noise is not a change.
    /// </summary>
    private const double BoundsTolerance = 0.01;

    private static readonly string[] FieldOrder =
    [
        "controlType",
        "name",
        "automationId",
        "bounds",
        "enabled",
        "visible",
        "focused",
        "selected",
        "expanded",
        "checked",
        "value",
        "toolTip",
    ];

    /// <summary>
    /// Compares <paramref name="before"/> (last success) with <paramref name="after"/> (failure).
    /// </summary>
    /// <param name="before">Baseline tree root.</param>
    /// <param name="after">Tree root captured on failure.</param>
    /// <returns>Added, removed, and changed nodes. Lists are empty when nothing moved.</returns>
    public static TreeDiff Diff(TreeNode before, TreeNode after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var beforeMap = Index(before);
        var afterMap = Index(after);
        var added = new List<TreeDiffEntry>();
        var changed = new List<TreeDiffChange>();
        foreach (var (key, afterNode) in afterMap)
        {
            if (!beforeMap.TryGetValue(key, out var beforeNode))
            {
                added.Add(new TreeDiffEntry { Path = key, Node = afterNode });
                continue;
            }

            var fields = ChangedFields(beforeNode, afterNode);
            if (fields.Count > 0)
            {
                changed.Add(
                    new TreeDiffChange
                    {
                        Path = key,
                        Fields = fields,
                        Before = beforeNode,
                        After = afterNode,
                    }
                );
            }
        }

        var removed = new List<TreeDiffEntry>();
        foreach (var (key, beforeNode) in beforeMap)
        {
            if (!afterMap.ContainsKey(key))
            {
                removed.Add(new TreeDiffEntry { Path = key, Node = beforeNode });
            }
        }

        return new TreeDiff
        {
            Added = added,
            Removed = removed,
            Changed = changed,
        };
    }

    private static Dictionary<string, TreeDiffNode> Index(TreeNode root)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        CountIds(root, counts);
        var map = new Dictionary<string, TreeDiffNode>(StringComparer.Ordinal);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        Walk(root, string.Empty, siblingIndex: 0, counts, seen, map);
        return map;
    }

    private static void CountIds(TreeNode node, Dictionary<string, int> counts)
    {
        if (!string.IsNullOrEmpty(node.AutomationId))
        {
            counts.TryGetValue(node.AutomationId, out var count);
            counts[node.AutomationId] = count + 1;
        }

        foreach (var child in node.Children)
        {
            CountIds(child, counts);
        }
    }

    private static void Walk(
        TreeNode node,
        string parentPath,
        int siblingIndex,
        Dictionary<string, int> counts,
        Dictionary<string, int> seen,
        Dictionary<string, TreeDiffNode> map
    )
    {
        var key = Key(node, parentPath, siblingIndex, counts, seen);
        map[key] = TreeDiffNode.From(node);
        for (var index = 0; index < node.Children.Count; index++)
        {
            Walk(node.Children[index], key, index, counts, seen, map);
        }
    }

    private static string Key(TreeNode node, string parentPath, int siblingIndex, Dictionary<string, int> counts, Dictionary<string, int> seen)
    {
        if (!string.IsNullOrEmpty(node.AutomationId) && counts.TryGetValue(node.AutomationId, out var count) && count == 1)
        {
            return node.AutomationId;
        }

        var segment = string.IsNullOrEmpty(node.AutomationId) ? "#" + node.ControlType + "@" + siblingIndex : node.AutomationId;
        var path = parentPath.Length == 0 ? segment : parentPath + "/" + segment;
        seen.TryGetValue(path, out var occurrence);
        seen[path] = occurrence + 1;
        return occurrence == 0 ? path : path + "~" + occurrence;
    }

    private static List<string> ChangedFields(TreeDiffNode before, TreeDiffNode after)
    {
        var fields = new List<string>();
        foreach (var field in FieldOrder)
        {
            if (!Same(field, before, after))
            {
                fields.Add(field);
            }
        }

        return fields;
    }

    private static bool Same(string field, TreeDiffNode before, TreeDiffNode after) =>
        field switch
        {
            "controlType" => string.Equals(before.ControlType, after.ControlType, StringComparison.Ordinal),
            "name" => string.Equals(before.Name, after.Name, StringComparison.Ordinal),
            "automationId" => string.Equals(before.AutomationId, after.AutomationId, StringComparison.Ordinal),
            "bounds" => SameBounds(before.Bounds, after.Bounds),
            "enabled" => before.Enabled == after.Enabled,
            "visible" => before.Visible == after.Visible,
            "focused" => before.Focused == after.Focused,
            "selected" => before.Selected == after.Selected,
            "expanded" => before.Expanded == after.Expanded,
            "checked" => before.Checked == after.Checked,
            "value" => string.Equals(before.Value, after.Value, StringComparison.Ordinal),
            "toolTip" => string.Equals(before.ToolTip, after.ToolTip, StringComparison.Ordinal),
            _ => true,
        };

    private static bool SameBounds(ElementBounds before, ElementBounds after) =>
        NearlyEqual(before.X, after.X)
        && NearlyEqual(before.Y, after.Y)
        && NearlyEqual(before.Width, after.Width)
        && NearlyEqual(before.Height, after.Height);

    private static bool NearlyEqual(double left, double right) => left == right || Math.Abs(left - right) <= BoundsTolerance;
}
