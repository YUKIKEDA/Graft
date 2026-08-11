using Graft.Protocol;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class LaunchTests
{
    /// <summary>
    /// LaunchAsync starts SampleWpfApp and getTree finds SampleButton.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp.csproj is available under the repo
    /// - GraftTest configuration builds with GRAFT_TEST
    ///
    /// Steps:
    /// - Application.LaunchAsync with the sample csproj
    /// - Poll GetTreeAsync until SampleButton appears
    /// - Dispose the session
    ///
    /// Expected:
    /// - SampleButton exists with name "Click Me"
    /// - Dispose completes without throwing
    /// </remarks>
    [Fact]
    public async Task Launch_SampleWpfApp_GetTreeFindsSampleButton()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(
            new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) }
        );

        var button = await WaitForSampleButtonAsync(session.Connection);
        Assert.Equal("Click Me", button.Name);
        Assert.True(session.ProcessId > 0);
    }

    private static async Task<TreeNode> WaitForSampleButtonAsync(AgentConnection connection)
    {
        GraftException? last = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                var tree = await connection.GetTreeAsync();
                var button = FindByAutomationId(tree.Root, "SampleButton");
                if (button is not null)
                {
                    return button;
                }

                last = new GraftException(
                    GraftErrorCodes.ElementNotFound,
                    "Element 'SampleButton' was not in the tree yet."
                );
            }
            catch (GraftException ex)
                when (ex.Code is GraftErrorCodes.ActionFailed or GraftErrorCodes.ElementNotFound)
            {
                last = ex;
            }

            await Task.Delay(100);
        }

        throw last
            ?? new GraftException(
                GraftErrorCodes.ElementNotFound,
                "Element 'SampleButton' not found."
            );
    }

    private static TreeNode? FindByAutomationId(TreeNode node, string automationId)
    {
        if (string.Equals(node.AutomationId, automationId, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindByAutomationId(child, automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
