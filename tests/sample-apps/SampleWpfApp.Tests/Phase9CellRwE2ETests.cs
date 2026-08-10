using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase9CellRwE2ETests
{
    /// <summary>
    /// GetCellText reads a virtualized DataGrid Text cell after scroll.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleGrid has 50 editable Name rows
    ///
    /// Steps:
    /// - GetCellTextAsync(40, 0) on SampleGrid
    ///
    /// Expected:
    /// - Text is "Row 40"
    /// </remarks>
    [Fact]
    public async Task GetCellText_VirtualizedRow_ReturnsDisplayText()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var text = await app.GetByAutomationId("SampleGrid").GetCellTextAsync(40, 0);
        Assert.Equal("Row 40", text);
    }

    /// <summary>
    /// SetCellValue then ExpectCellText updates a Text cell via CommitEdit.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleGrid Name column is editable (TwoWay)
    ///
    /// Steps:
    /// - SetCellValueAsync(35, 0, "Edited 35")
    /// - ExpectCellTextAsync(35, 0, "Edited 35")
    ///
    /// Expected:
    /// - Cell text matches after commit
    /// </remarks>
    [Fact]
    public async Task SetCellValue_ThenExpectCellText_UpdatesRow()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleGrid").SetCellValueAsync(35, 0, "Edited 35");
        await app.GetByAutomationId("SampleGrid").ExpectCellTextAsync(35, 0, "Edited 35");
    }
}
