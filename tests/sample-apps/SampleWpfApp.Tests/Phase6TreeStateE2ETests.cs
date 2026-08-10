using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase6TreeStateE2ETests
{
    /// <summary>
    /// select then ExpectSelected on the realized list item (tree state, not StatusText).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp virtualized SampleList with ListItem-xx automation ids
    ///
    /// Steps:
    /// - SelectAsync(35) on SampleList
    /// - ExpectSelectedAsync(true) on ListItem-35
    /// - ExpectSelectedAsync(false) on a different realized item after scroll (optional path: Item 0 if realized)
    ///
    /// Expected:
    /// - Selected item reports selected=true via tree Expect
    /// </remarks>
    [Fact]
    public async Task Select_ThenExpectSelected_OnListItem()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleList").SelectAsync(35);
        await app.GetByAutomationId("ListItem-35").ExpectSelectedAsync(true);
    }

    /// <summary>
    /// Expand / Collapse then ExpectExpanded on SampleTreeRoot.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleTreeRoot starts collapsed
    ///
    /// Steps:
    /// - ExpectExpandedAsync(false)
    /// - ExpandAsync → ExpectExpandedAsync(true)
    /// - CollapseAsync → ExpectExpandedAsync(false)
    ///
    /// Expected:
    /// - Tree expanded state matches Expect
    /// </remarks>
    [Fact]
    public async Task ExpandCollapse_ThenExpectExpanded_OnTreeRoot()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleTreeRoot").ExpectExpandedAsync(false);
        await app.GetByAutomationId("SampleTreeRoot").ExpandAsync();
        await app.GetByAutomationId("SampleTreeRoot").ExpectExpandedAsync(true);
        await app.GetByAutomationId("SampleTreeRoot").CollapseAsync();
        await app.GetByAutomationId("SampleTreeRoot").ExpectExpandedAsync(false);
    }
}
