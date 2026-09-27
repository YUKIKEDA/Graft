using System.Text.Json;
using Graft.Core.Diagnostics;
using Graft.Core.Selectors;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

/// <summary>
/// Guards the diagnostic tree diff attached to failure reports.
/// </summary>
public sealed class TreeDifferTests
{
    /// <summary>
    /// A new child is added, a missing child is removed, and a renamed node is changed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Baseline window with Status and Save
    /// - Failure tree drops Save, renames Status, and adds Cancel
    ///
    /// Steps:
    /// - TreeDiffer.Diff
    ///
    /// Expected:
    /// - added is Cancel, removed is Save, changed is Status with field name
    /// </remarks>
    [Fact]
    public void Diff_ReportsAddedRemovedAndChanged()
    {
        var before = Window(Node("Status", "Ready", "TextBlock"), Node("Save", "Save"));
        var after = Window(Node("Status", "Busy", "TextBlock"), Node("Cancel", "Cancel"));

        var diff = TreeDiffer.Diff(before, after);

        var added = Assert.Single(diff.Added);
        Assert.Equal("Cancel", added.Path);
        Assert.Equal("Cancel", added.Node.Name);
        var removed = Assert.Single(diff.Removed);
        Assert.Equal("Save", removed.Path);
        var changed = Assert.Single(diff.Changed);
        Assert.Equal("Status", changed.Path);
        Assert.Equal(["name"], changed.Fields);
        Assert.Equal("Ready", changed.Before.Name);
        Assert.Equal("Busy", changed.After.Name);
    }

    /// <summary>
    /// runtimeId changes are not attributes of the diagnostic diff.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Two trees with the same automation ids and names
    /// - Different RuntimeId values
    ///
    /// Steps:
    /// - TreeDiffer.Diff
    ///
    /// Expected:
    /// - added, removed, and changed are empty
    /// </remarks>
    [Fact]
    public void Diff_IgnoresRuntimeId()
    {
        var before = Window(Node("Save", "Save", "Button", runtimeId: 1));
        var after = Window(Node("Save", "Save", "Button", runtimeId: 99));

        var diff = TreeDiffer.Diff(before, after);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Changed);
    }

    /// <summary>
    /// Nodes without a unique automation id are keyed by a path from the root.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Two sibling buttons share automation id Item
    /// - One text block has an empty automation id
    ///
    /// Steps:
    /// - TreeDiffer.Diff against a copy that renames the second Item and the text block
    ///
    /// Expected:
    /// - duplicate ids use Window/Item and Window/Item~1
    /// - the text block path is Window/#TextBlock@2 and its name change is a field change
    /// </remarks>
    [Fact]
    public void Diff_UsesPathWhenAutomationIdIsMissingOrDuplicated()
    {
        var before = Window(Node("Item", "One"), Node("Item", "Two"), Node(string.Empty, "Hello", "TextBlock"));
        var after = Window(Node("Item", "One"), Node("Item", "Three"), Node(string.Empty, "Hello!", "TextBlock"));

        var diff = TreeDiffer.Diff(before, after);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Equal(2, diff.Changed.Count);
        Assert.Equal(["name"], Assert.Single(diff.Changed, change => change.Path == "Window/Item~1").Fields);
        Assert.Equal(["name"], Assert.Single(diff.Changed, change => change.Path == "Window/#TextBlock@2").Fields);
    }

    /// <summary>
    /// A unique automation id still matches after its parent changes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Save is unique and moves from PanelA to PanelB
    /// - Save's own attributes stay the same
    ///
    /// Steps:
    /// - TreeDiffer.Diff
    ///
    /// Expected:
    /// - PanelA is removed and PanelB is added
    /// - Save is not added, removed, or changed
    /// </remarks>
    [Fact]
    public void Diff_MatchesUniqueAutomationIdAcrossParents()
    {
        var before = Window(Node("PanelA", "A", "Panel", Node("Save", "Save")));
        var after = Window(Node("PanelB", "B", "Panel", Node("Save", "Save")));

        var diff = TreeDiffer.Diff(before, after);

        Assert.Equal("PanelB", Assert.Single(diff.Added).Path);
        Assert.Equal("PanelA", Assert.Single(diff.Removed).Path);
        Assert.DoesNotContain(diff.Added, entry => entry.Path == "Save");
        Assert.DoesNotContain(diff.Removed, entry => entry.Path == "Save");
        Assert.DoesNotContain(diff.Changed, change => change.Path == "Save");
    }

    /// <summary>
    /// treeDiff round-trips, and a report without one omits the property.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - FailureReport with a one-node treeDiff
    /// - FailureReport with treeDiff left null
    ///
    /// Steps:
    /// - Serialize then Deserialize
    /// - Serialize the null report and inspect JSON
    ///
    /// Expected:
    /// - path and fields survive
    /// - treeDiff key is absent when null
    /// </remarks>
    [Fact]
    public void Serialize_RoundTripsTreeDiff_AndOmitsWhenNull()
    {
        var diff = TreeDiffer.Diff(Window(Node("Status", "Ready", "TextBlock")), Window(Node("Status", "Busy", "TextBlock")));
        var original = new FailureReport
        {
            Step = FailureSteps.ExpectName,
            TimedOut = true,
            Selector = new FailureReportSelector { AutomationId = "Status" },
            TreeDiff = diff,
        };

        var decoded = FailureReportJson.Deserialize(FailureReportJson.Serialize(original));

        var changed = Assert.Single(decoded.TreeDiff!.Changed);
        Assert.Equal("Status", changed.Path);
        Assert.Equal(["name"], changed.Fields);

        var without = new FailureReport
        {
            Step = FailureSteps.Invoke,
            TimedOut = false,
            Selector = new FailureReportSelector { AutomationId = "Save" },
        };
        using var doc = JsonDocument.Parse(FailureReportJson.Serialize(without));
        Assert.False(doc.RootElement.TryGetProperty("treeDiff", out _));
    }

    /// <summary>
    /// The session baseline stays empty until a success, and can be turned off.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - TreeBaseline with no remembered tree
    /// - A second baseline with Enabled false
    ///
    /// Steps:
    /// - Diff before Remember
    /// - Remember while disabled, then enable and Diff
    /// - Remember while enabled, then Diff a renamed tree
    ///
    /// Expected:
    /// - no baseline and a disabled baseline produce null
    /// - an enabled baseline reports the name change
    /// </remarks>
    [Fact]
    public void Baseline_DiffsOnlyAfterEnabledSuccess()
    {
        var baseline = new TreeBaseline();
        var before = Window(Node("Status", "Ready", "TextBlock"));
        var after = Window(Node("Status", "Busy", "TextBlock"));

        Assert.Null(baseline.Diff(after));

        var disabled = new TreeBaseline { Enabled = false };
        disabled.Remember(before);
        disabled.Enabled = true;
        Assert.Null(disabled.Diff(after));

        baseline.Remember(before);
        var changed = Assert.Single(baseline.Diff(after)!.Changed);
        Assert.Equal("Status", changed.Path);
        Assert.Equal(["name"], changed.Fields);
    }

    private static TreeNode Window(params TreeNode[] children) => Node("Window", "Main", "Window", children);

    private static TreeNode Node(string automationId, string name, string controlType = "Button", params TreeNode[] children) =>
        Node(automationId, name, controlType, runtimeId: 0, children);

    private static TreeNode Node(string automationId, string name, string controlType, int runtimeId, params TreeNode[] children) =>
        new()
        {
            RuntimeId = runtimeId,
            ControlType = controlType,
            Name = name,
            AutomationId = automationId,
            Bounds = new ElementBounds
            {
                X = 0,
                Y = 0,
                Width = 10,
                Height = 10,
            },
            Enabled = true,
            Visible = true,
            Children = children,
        };
}
