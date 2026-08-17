using Graft.Core;
using Graft.Protocol;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 19 ListBox selectMany acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase19SelectManyE2ETests
{
    /// <summary>
    /// SelectManyAsync selects multiple items and updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - SampleMultiList uses SelectionMode=Extended
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleMultiList").SelectManyAsync([1, 3])
    /// - ExpectSelectedAsync(true) on MultiListItem-01 and MultiListItem-03
    /// - ExpectNameAsync("Multi 2") on StatusText
    ///
    /// Expected:
    /// - Two items selected; StatusText is Multi 2
    /// </remarks>
    [Fact]
    public async Task SelectMany_SampleMultiList_SelectsTwoItems()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleMultiList").SelectManyAsync([1, 3]);
        await app.GetByAutomationId("MultiListItem-01").ExpectSelectedAsync(true);
        await app.GetByAutomationId("MultiListItem-03").ExpectSelectedAsync(true);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Multi 2");
    }

    /// <summary>
    /// SelectManyAsync with empty indexes clears selection.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleMultiList supports Extended selection
    ///
    /// Steps:
    /// - SelectManyAsync([0, 1]) then SelectManyAsync([])
    /// - ExpectSelectedAsync(false) on MultiListItem-00
    /// - ExpectNameAsync("Multi 0") on StatusText
    ///
    /// Expected:
    /// - Selection cleared; StatusText is Multi 0
    /// </remarks>
    [Fact]
    public async Task SelectMany_EmptyIndexes_ClearsSelection()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleMultiList").SelectManyAsync([0, 1]);
        await app.GetByAutomationId("SampleMultiList").SelectManyAsync([]);
        await app.GetByAutomationId("MultiListItem-00").ExpectSelectedAsync(false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Multi 0");
    }

    /// <summary>
    /// SelectManyAsync on SelectionMode=Single ListBox fails clearly.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleList remains SelectionMode=Single
    ///
    /// Steps:
    /// - SelectManyAsync([1]) on SampleList
    ///
    /// Expected:
    /// - GraftException with action.failed
    /// </remarks>
    [Fact]
    public async Task SelectMany_SampleListSingleMode_Fails()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var ex = await Assert.ThrowsAsync<GraftException>(() => app.GetByAutomationId("SampleList").SelectManyAsync([1]));
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
    }
}
