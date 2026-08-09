using Graft.Core.Selectors;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

public sealed class SelectorHealerTests
{
    /// <summary>
    /// Dropping a stale AutomationId heals via Name + Near when that subset is unique.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Tree: Window(Main) → Button(Click Me / SampleButton)
    /// - Failed selector has wrong AutomationId plus correct Name / ControlType / Near
    ///
    /// Steps:
    /// - TryGetAutoHeal
    ///
    /// Expected:
    /// - true; healed selector has no AutomationId and matches Name + Near (and ControlType)
    /// </remarks>
    [Fact]
    public void TryGetAutoHeal_StaleAutomationId_RelaxesToNameNear()
    {
        var root = SampleTree();
        var failed = new Selector
        {
            AutomationId = "GoneButton",
            Name = "Click Me",
            ControlType = "Button",
            NearAutomationId = "Main",
        };

        Assert.True(SelectorHealer.TryGetAutoHeal(root, failed, out var healed));
        Assert.True(string.IsNullOrWhiteSpace(healed.AutomationId));
        Assert.Equal("Click Me", healed.Name);
        Assert.Equal("Main", healed.NearAutomationId);
        var node = TreeSelector.Resolve(root, healed);
        Assert.Equal("SampleButton", node.AutomationId);
    }

    /// <summary>
    /// AutomationId-only miss yields stable-identity candidates but no unique auto-heal
    /// when multiple named controls exist.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample tree with Button and Status Text under Main
    ///
    /// Steps:
    /// - ProposeCandidates for ByAutomationId("Missing")
    /// - TryGetAutoHeal
    ///
    /// Expected:
    /// - Candidates non-empty; TryGetAutoHeal false (tied stable identities)
    /// </remarks>
    [Fact]
    public void ProposeCandidates_MissingAutomationId_ListsStableIdentities_WithoutAutoHeal()
    {
        var root = SampleTree();
        var failed = Selector.ByAutomationId("Missing");
        var candidates = SelectorHealer.ProposeCandidates(root, failed);
        Assert.NotEmpty(candidates);
        Assert.False(SelectorHealer.TryGetAutoHeal(root, failed, out _));
    }

    /// <summary>
    /// A single stable-identity candidate allows auto-heal for AutomationId-only miss.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Tree with only one named actionable child under Main
    ///
    /// Steps:
    /// - TryGetAutoHeal ByAutomationId("Missing")
    ///
    /// Expected:
    /// - true; resolves to the only button
    /// </remarks>
    [Fact]
    public void TryGetAutoHeal_SingleStableIdentity_Succeeds()
    {
        var root = Node(
            "Window",
            "Sample",
            "Main",
            [Node("Button", "Click Me", "SampleButton", [])]
        );
        Assert.True(
            SelectorHealer.TryGetAutoHeal(root, Selector.ByAutomationId("Missing"), out var healed)
        );
        Assert.Equal("SampleButton", TreeSelector.Resolve(root, healed).AutomationId);
    }

    private static TreeNode SampleTree() =>
        Node(
            "Window",
            "Sample",
            "Main",
            [Node("Button", "Click Me", "SampleButton", []), Node("Text", "Idle", "StatusText", [])]
        );

    private static TreeNode Node(
        string controlType,
        string name,
        string automationId,
        TreeNode[] children
    ) =>
        new()
        {
            RuntimeId = automationId.GetHashCode(StringComparison.Ordinal),
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
            Focused = false,
            Children = children,
        };
}
