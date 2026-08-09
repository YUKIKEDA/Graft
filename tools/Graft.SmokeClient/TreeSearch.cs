using Graft.Protocol.Messages;

namespace Graft.SmokeClient;

internal static class TreeSearch
{
    public const string SampleButtonAutomationId = "SampleButton";

    public const string StatusTextAutomationId = "StatusText";

    public static TreeNode? FindByAutomationId(TreeNode node, string automationId)
    {
        if (string.Equals(node.AutomationId, automationId, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindByAutomationId(child, automationId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
