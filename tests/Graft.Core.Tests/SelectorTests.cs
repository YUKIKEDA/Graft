using Graft.Core.Selectors;
using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

public sealed class SelectorTests
{
    /// <summary>
    /// ByAutomationId resolves a unique matching node.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Synthetic tree with SampleButton
    ///
    /// Steps:
    /// - TreeSelector.Resolve with Selector.ByAutomationId("SampleButton")
    ///
    /// Expected:
    /// - Returned node AutomationId is SampleButton and Name is Click Me
    /// </remarks>
    [Fact]
    public void Resolve_ByAutomationId_ReturnsUniqueMatch()
    {
        var root = SampleTree();
        var node = TreeSelector.Resolve(root, Selector.ByAutomationId("SampleButton"));
        Assert.Equal("SampleButton", node.AutomationId);
        Assert.Equal("Click Me", node.Name);
    }

    /// <summary>
    /// Name-only selector scores below threshold and yields notFound.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Synthetic tree with a named button
    ///
    /// Steps:
    /// - Resolve with Name = "Click Me" only
    ///
    /// Expected:
    /// - GraftException with element.notFound (score 40 &lt; threshold 60)
    /// </remarks>
    [Fact]
    public void Resolve_NameOnly_ThrowsElementNotFound()
    {
        var root = SampleTree();
        var ex = Assert.Throws<GraftException>(() =>
            TreeSelector.Resolve(root, new Selector { Name = "Click Me" })
        );
        Assert.Equal(GraftErrorCodes.ElementNotFound, ex.Code);
    }

    /// <summary>
    /// Name + near-path reaches the threshold and resolves.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Tree: Window(Main) → Button(Click Me)
    ///
    /// Steps:
    /// - Resolve with Name and NearAutomationId=Main
    ///
    /// Expected:
    /// - Matched button (score 40+20 = 60)
    /// </remarks>
    [Fact]
    public void Resolve_NamePlusNearPath_ReturnsMatch()
    {
        var root = SampleTree();
        var node = TreeSelector.Resolve(
            root,
            new Selector { Name = "Click Me", NearAutomationId = "Main" }
        );
        Assert.Equal("SampleButton", node.AutomationId);
    }

    /// <summary>
    /// Two nodes with the same best score throw element.ambiguous.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Two buttons share AutomationId Dup
    ///
    /// Steps:
    /// - Resolve ByAutomationId("Dup")
    ///
    /// Expected:
    /// - GraftException with element.ambiguous
    /// </remarks>
    [Fact]
    public void Resolve_TiedBestScore_ThrowsElementAmbiguous()
    {
        var root = Node(
            "Window",
            "Root",
            "Root",
            [Node("Button", "A", "Dup", []), Node("Button", "B", "Dup", [])]
        );

        var ex = Assert.Throws<GraftException>(() =>
            TreeSelector.Resolve(root, Selector.ByAutomationId("Dup"))
        );
        Assert.Equal(GraftErrorCodes.ElementAmbiguous, ex.Code);
    }

    /// <summary>
    /// Missing automation id yields element.notFound.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample tree without MissingId
    ///
    /// Steps:
    /// - Resolve ByAutomationId("MissingId")
    ///
    /// Expected:
    /// - GraftException with element.notFound
    /// </remarks>
    [Fact]
    public void Resolve_MissingAutomationId_ThrowsElementNotFound()
    {
        var ex = Assert.Throws<GraftException>(() =>
            TreeSelector.Resolve(SampleTree(), Selector.ByAutomationId("MissingId"))
        );
        Assert.Equal(GraftErrorCodes.ElementNotFound, ex.Code);
    }

    /// <summary>
    /// A specified AutomationId that does not match hard-fails even when Name/Near match.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample tree with Click Me under Main
    ///
    /// Steps:
    /// - Resolve with wrong AutomationId plus correct Name and NearAutomationId
    ///
    /// Expected:
    /// - element.notFound (Phase 4: AutomationId is hard when present)
    /// </remarks>
    [Fact]
    public void Resolve_WrongAutomationId_WithMatchingNameNear_ThrowsElementNotFound()
    {
        var ex = Assert.Throws<GraftException>(() =>
            TreeSelector.Resolve(
                SampleTree(),
                new Selector
                {
                    AutomationId = "Gone",
                    Name = "Click Me",
                    NearAutomationId = "Main",
                }
            )
        );
        Assert.Equal(GraftErrorCodes.ElementNotFound, ex.Code);
    }

    /// <summary>
    /// Empty selector is selector.invalid.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty Selector instance
    ///
    /// Steps:
    /// - TreeSelector.Resolve
    ///
    /// Expected:
    /// - GraftException with selector.invalid
    /// </remarks>
    [Fact]
    public void Resolve_EmptySelector_ThrowsSelectorInvalid()
    {
        var ex = Assert.Throws<GraftException>(() =>
            TreeSelector.Resolve(SampleTree(), new Selector())
        );
        Assert.Equal(GraftErrorCodes.SelectorInvalid, ex.Code);
    }

    /// <summary>
    /// Score helper adds automationId / name / controlType / near weights.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Button node under ancestor Main
    ///
    /// Steps:
    /// - TreeSelector.Score with all criteria matching
    ///
    /// Expected:
    /// - Score equals 100+40+15+20
    /// </remarks>
    [Fact]
    public void Score_AllCriteriaMatch_SumsWeights()
    {
        var button = Node("Button", "Click Me", "SampleButton", []);
        var score = TreeSelector.Score(
            button,
            new Selector
            {
                AutomationId = "SampleButton",
                Name = "Click Me",
                ControlType = "Button",
                NearAutomationId = "Main",
            },
            ancestorAutomationIds: ["Main"]
        );
        Assert.Equal(
            SelectorWeights.AutomationId
                + SelectorWeights.Name
                + SelectorWeights.ControlType
                + SelectorWeights.NearPath,
            score
        );
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
