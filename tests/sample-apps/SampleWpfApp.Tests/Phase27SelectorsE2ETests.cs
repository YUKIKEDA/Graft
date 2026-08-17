using Graft.Core;
using Graft.Core.Selectors;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 27 selector / path / key acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase27SelectorsE2ETests
{
    /// <summary>
    /// GetByName hard-matches SampleButton and invokes it.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleButton Automation Name is SampleClickMe
    ///
    /// Steps:
    /// - GetByName("SampleClickMe").InvokeAsync
    /// - ExpectNameAsync("Clicked 1") on StatusText
    ///
    /// Expected:
    /// - StatusText is Clicked 1
    /// </remarks>
    [Fact]
    public async Task GetByName_SampleClickMe_InvokesSampleButton()
    {
        await using var app = await LaunchAsync();
        await app.GetByName("SampleClickMe").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
    }

    /// <summary>
    /// SelectAsync(key) selects a virtualized list item by name.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleList items expose AutomationProperties.Name = Item N
    ///
    /// Steps:
    /// - SelectAsync("Item 35") on SampleList
    /// - ExpectNameAsync("Selected Item 35") on StatusText
    ///
    /// Expected:
    /// - StatusText is Selected Item 35
    /// </remarks>
    [Fact]
    public async Task SelectByKey_ListItemName_UpdatesStatus()
    {
        await using var app = await LaunchAsync();
        await app.GetByAutomationId("SampleList").SelectAsync("Item 35");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Selected Item 35");
    }

    /// <summary>
    /// SelectTreeAsync expands and selects a deep TreeView leaf.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleTree has Root/ChildA/Grandchild
    ///
    /// Steps:
    /// - SelectTreeAsync path on SampleTree
    /// - ExpectSelectedAsync(true) on SampleTreeGrandchild
    ///
    /// Expected:
    /// - Grandchild is selected
    /// </remarks>
    [Fact]
    public async Task SelectTree_DeepPath_SelectsGrandchild()
    {
        await using var app = await LaunchAsync();
        await app.GetByAutomationId("SampleTree").SelectTreeAsync("SampleTreeRoot/SampleTreeChildA/SampleTreeGrandchild");
        await app.GetByAutomationId("SampleTreeGrandchild").ExpectSelectedAsync(true);
    }

    /// <summary>
    /// Child + Nth relative selectors target RelativeHost buttons.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - RelativeHost has RelA and RelB buttons
    ///
    /// Steps:
    /// - ChildByName("RelA").InvokeAsync
    /// - Child(ByControlType Button).Nth(1).InvokeAsync
    ///
    /// Expected:
    /// - StatusText RelA then RelB
    /// </remarks>
    [Fact]
    public async Task Relative_ChildAndNth_InvokesButtons()
    {
        await using var app = await LaunchAsync();
        await app.GetByAutomationId("RelativeHost").ChildByName("RelA").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("RelA");

        await app.GetByAutomationId("RelativeHost").Child(Selector.ByControlType("Button")).Nth(1).InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("RelB");
    }

    private static Task<GraftSession> LaunchAsync() =>
        Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );
}
