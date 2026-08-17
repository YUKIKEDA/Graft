using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 28 DataGrid advanced acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase28DataGridE2ETests
{
    /// <summary>
    /// Template column R/W, SelectCell, sort+SelectRow, AddRow/DeleteSelectedRows.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SamplePhase28Grid exposes Name/Active/Notes with CellOrRowHeader selection
    ///
    /// Steps:
    /// - Get/Set Notes template cell
    /// - SelectCell(1, Name) → StatusText Phase28Cell
    /// - ClickColumnHeader(Name) then SelectRow(Name, P28-5) → row selected
    /// - AddRow then SelectRow(Name, New) then DeleteSelectedRows → SelectRow fails
    ///
    /// Expected:
    /// - Template and Phase 28 APIs update Status / selection as specified
    /// </remarks>
    [Fact]
    public async Task Phase28_TemplateSelectSortCrud_Works()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var grid = app.GetByAutomationId("SamplePhase28Grid");

        var notes = await grid.GetCellTextAsync(0, "Notes");
        Assert.Equal("N0", notes);
        await grid.SetCellValueAsync(0, "Notes", "Hello");
        await grid.ExpectCellTextAsync(0, "Notes", "Hello");

        await grid.SelectCellAsync(1, "Name");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Phase28Cell 1:Name");

        await grid.ClickColumnHeaderAsync("Name");
        await grid.SelectRowAsync("Name", "P28-5");
        await app.GetByAutomationId("Phase28Row-05").ExpectSelectedAsync(true);

        await grid.AddRowAsync();
        await grid.SelectRowAsync("Name", "New");
        await grid.DeleteSelectedRowsAsync();

        var thrown = await Assert.ThrowsAsync<GraftException>(async () => await grid.SelectRowAsync("Name", "New"));
        Assert.Equal(Graft.Protocol.GraftErrorCodes.ElementNotFound, thrown.Code);
    }
}
