using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 21 DataGrid columnKey + CheckBox column acceptance.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase21DataGridColumnKeyE2ETests
{
    /// <summary>
    /// GetCellText by column Header reads the Name cell.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleGrid has Header "Name" Text column
    ///
    /// Steps:
    /// - GetCellTextAsync(40, "Name") on SampleGrid
    ///
    /// Expected:
    /// - Text is "Row 40"
    /// </remarks>
    [Fact]
    public async Task GetCellText_ByColumnKey_ReturnsName()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var text = await app.GetByAutomationId("SampleGrid").GetCellTextAsync(40, "Name");
        Assert.Equal("Row 40", text);
    }

    /// <summary>
    /// SetCellValue / ExpectCellText toggle Active CheckBox via columnKey.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleGrid has Header "Active" CheckBox column
    ///
    /// Steps:
    /// - SetCellValueAsync(35, "Active", "True")
    /// - ExpectCellTextAsync(35, "Active", "True")
    ///
    /// Expected:
    /// - CheckBox cell reports True
    /// </remarks>
    [Fact]
    public async Task SetCellValue_CheckBoxByColumnKey_UpdatesActive()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleGrid").SetCellValueAsync(35, "Active", "True");
        await app.GetByAutomationId("SampleGrid").ExpectCellTextAsync(35, "Active", "True");
    }
}
