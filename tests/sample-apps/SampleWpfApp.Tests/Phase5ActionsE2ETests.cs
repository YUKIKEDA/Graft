using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase5ActionsE2ETests
{
    /// <summary>
    /// scrollIntoView(index) realizes a virtualized list item and returns its identity.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp has virtualized SampleList with 50 items
    ///
    /// Steps:
    /// - Launch sample
    /// - ScrollIntoViewAsync(40) on SampleList
    /// - ExpectNameAsync("Item 40") on returned automationId
    ///
    /// Expected:
    /// - Identity automationId is ListItem-40; tree name is Item 40
    /// </remarks>
    [Fact]
    public async Task ScrollIntoView_VirtualizedListIndex_ReturnsIdentity()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var identity = await app.GetByAutomationId("SampleList").ScrollIntoViewAsync(40);
        Assert.Equal("ListItem-40", identity.AutomationId);
        await app.GetByAutomationId(identity.AutomationId).ExpectNameAsync("Item 40");
    }

    /// <summary>
    /// select(index) selects a virtualized list item and updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp SampleList SelectionChanged updates StatusText
    ///
    /// Steps:
    /// - Launch sample
    /// - SelectAsync(35) on SampleList
    /// - ExpectNameAsync("Selected Item 35") on StatusText
    ///
    /// Expected:
    /// - StatusText reflects the selected item
    /// </remarks>
    [Fact]
    public async Task Select_VirtualizedListIndex_UpdatesStatus()
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
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Selected Item 35");
    }

    /// <summary>
    /// select on ComboBox updates StatusText via SelectionChanged.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleCombo has ComboItem items
    ///
    /// Steps:
    /// - SelectAsync(1) on SampleCombo
    /// - Expect StatusText "Combo Beta"
    ///
    /// Expected:
    /// - StatusText is Combo Beta
    /// </remarks>
    [Fact]
    public async Task Select_ComboBoxIndex_UpdatesStatus()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleCombo").SelectAsync(1);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Combo Beta");
    }

    /// <summary>
    /// Expand / Collapse on SampleTreeRoot updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleTreeRoot starts collapsed (default)
    ///
    /// Steps:
    /// - ExpandAsync SampleTreeRoot → Expect Expanded
    /// - CollapseAsync SampleTreeRoot → Expect Collapsed
    ///
    /// Expected:
    /// - StatusText toggles Expanded / Collapsed
    /// </remarks>
    [Fact]
    public async Task ExpandCollapse_TreeRoot_UpdatesStatus()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleTreeRoot").ExpandAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Expanded");

        await app.GetByAutomationId("SampleTreeRoot").CollapseAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Collapsed");
    }
}
