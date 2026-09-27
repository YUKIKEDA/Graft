using Graft.Instrumentation.Tree;
using Graft.Protocol.Messages;

namespace Graft.TestSupport;

/// <summary>
/// Tree provider that returns one button named Click Me.
/// </summary>
internal sealed class FakeTreeProvider : IUiTreeProvider
{
    public GetTreeResult GetTree(GetTreeOptions options) =>
        new()
        {
            Truncated = false,
            Root = new TreeNode
            {
                RuntimeId = 1,
                ControlType = "Button",
                Name = "Click Me",
                AutomationId = "SampleButton",
                Bounds = new ElementBounds
                {
                    X = 10,
                    Y = 20,
                    Width = 80,
                    Height = 24,
                },
                Enabled = true,
                Visible = true,
                Focused = false,
                Children = Array.Empty<TreeNode>(),
            },
        };
}
