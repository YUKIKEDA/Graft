using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase8DataGridE2ETests
{
    /// <summary>
    /// scrollIntoView(index) realizes a virtualized DataGrid row and returns its identity.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp has virtualized SampleGrid with 50 rows (GridRow-xx)
    ///
    /// Steps:
    /// - Launch sample
    /// - ScrollIntoViewAsync(40) on SampleGrid
    /// - ExpectNameAsync("Row 40") on returned automationId
    ///
    /// Expected:
    /// - Identity automationId is GridRow-40; tree name is Row 40
    /// </remarks>
    [Fact]
    public async Task ScrollIntoView_VirtualizedGridRow_ReturnsIdentity()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var identity = await app.GetByAutomationId("SampleGrid").ScrollIntoViewAsync(40);
        Assert.Equal("GridRow-40", identity.AutomationId);
        await app.GetByAutomationId(identity.AutomationId).ExpectNameAsync("Row 40");
    }

    /// <summary>
    /// select(index) selects a DataGrid row; ExpectSelected reads tree selected on the row.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleGrid uses SelectionUnit=FullRow / SelectionMode=Single
    ///
    /// Steps:
    /// - SelectAsync(35) on SampleGrid
    /// - ExpectNameAsync("Grid Row 35") on StatusText
    /// - ExpectSelectedAsync(true) on GridRow-35
    ///
    /// Expected:
    /// - StatusText and tree selected state reflect the row
    /// </remarks>
    [Fact]
    public async Task Select_ThenExpectSelected_OnGridRow()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleGrid").SelectAsync(35);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Grid Row 35");
        await app.GetByAutomationId("GridRow-35").ExpectSelectedAsync(true);
    }

    /// <summary>
    /// toggle then ExpectChecked on SampleCheckBox (tree checked, not Content).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleCheckBox starts unchecked
    ///
    /// Steps:
    /// - ExpectCheckedAsync(false)
    /// - ToggleAsync → ExpectCheckedAsync(true)
    /// - ToggleAsync → ExpectCheckedAsync(false)
    ///
    /// Expected:
    /// - Tree checked state matches Expect
    /// </remarks>
    [Fact]
    public async Task Toggle_ThenExpectChecked_OnCheckBox()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleCheckBox").ExpectCheckedAsync(false);
        await app.GetByAutomationId("SampleCheckBox").ToggleAsync();
        await app.GetByAutomationId("SampleCheckBox").ExpectCheckedAsync(true);
        await app.GetByAutomationId("SampleCheckBox").ToggleAsync();
        await app.GetByAutomationId("SampleCheckBox").ExpectCheckedAsync(false);
    }
}
