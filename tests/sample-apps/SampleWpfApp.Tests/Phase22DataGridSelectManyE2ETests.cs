using Graft.Core;
using Graft.Protocol;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 22 DataGrid selectMany acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase22DataGridSelectManyE2ETests
{
    /// <summary>
    /// SelectManyAsync selects multiple DataGrid rows and updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - SampleMultiGrid uses SelectionMode=Extended and SelectionUnit=FullRow
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleMultiGrid").SelectManyAsync([1, 3])
    /// - ExpectSelectedAsync(true) on MultiGridRow-01 and MultiGridRow-03
    /// - ExpectNameAsync("MultiGrid 2") on StatusText
    ///
    /// Expected:
    /// - Two rows selected; StatusText is MultiGrid 2
    /// </remarks>
    [Fact]
    public async Task SelectMany_SampleMultiGrid_SelectsTwoRows()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleMultiGrid").SelectManyAsync([1, 3]);
        await app.GetByAutomationId("MultiGridRow-01").ExpectSelectedAsync(true);
        await app.GetByAutomationId("MultiGridRow-03").ExpectSelectedAsync(true);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MultiGrid 2");
    }

    /// <summary>
    /// SelectManyAsync with empty indexes clears DataGrid selection.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleMultiGrid supports Extended selection
    ///
    /// Steps:
    /// - SelectManyAsync([0, 1]) then SelectManyAsync([])
    /// - ExpectSelectedAsync(false) on MultiGridRow-00
    /// - ExpectNameAsync("MultiGrid 0") on StatusText
    ///
    /// Expected:
    /// - Selection cleared; StatusText is MultiGrid 0
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

        await app.GetByAutomationId("SampleMultiGrid").SelectManyAsync([0, 1]);
        await app.GetByAutomationId("SampleMultiGrid").SelectManyAsync([]);
        await app.GetByAutomationId("MultiGridRow-00").ExpectSelectedAsync(false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MultiGrid 0");
    }

    /// <summary>
    /// SelectManyAsync on SelectionMode=Single DataGrid fails clearly.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleGrid remains SelectionMode=Single
    ///
    /// Steps:
    /// - SelectManyAsync([1]) on SampleGrid
    ///
    /// Expected:
    /// - GraftException with action.failed
    /// </remarks>
    [Fact]
    public async Task SelectMany_SampleGridSingleMode_Fails()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var ex = await Assert.ThrowsAsync<GraftException>(() =>
            app.GetByAutomationId("SampleGrid").SelectManyAsync([1])
        );
        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
    }
}
