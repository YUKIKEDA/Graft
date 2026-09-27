using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Collections category E2E (DataGrid / List* / Tree*).
/// </summary>
[Collection(GalleryCollectionsCollection.Name)]
public sealed class CollectionsE2ETests
{
    private readonly GalleryCollectionsFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Collections fixture.</param>
    public CollectionsE2ETests(GalleryCollectionsFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// DataGrid select and screenshot.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - DataGridPage_Main
    ///
    /// Steps:
    /// - Navigate Nav_DataGrid
    /// - SelectAsync(0) best-effort
    /// - Screenshot
    ///
    /// Expected:
    /// - Grid visible; PNG captured
    /// </remarks>
    [Fact]
    public async Task DataGrid_Select_AndScreenshot()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Collections", "Nav_DataGrid");
        await (await GalleryNav.BringAsync(app, "DataGridPage_Main")).SelectAsync(0);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// ListBox and ListView selection.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ListBoxPage_Main / ListViewPage_Main / ListViewPage_SelectionMode
    ///
    /// Steps:
    /// - Navigate ListBox → SelectAsync
    /// - Navigate ListView → SelectAsync + SelectionMode Select
    /// - Screenshot
    ///
    /// Expected:
    /// - Selections succeed
    /// </remarks>
    [Fact]
    public async Task ListBoxAndListView_Select()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Collections", "Nav_ListBox");
        await (await GalleryNav.BringAsync(app, "ListBoxPage_Main")).SelectAsync(1);
        await GalleryNav.NavigateAsync(app, "Nav_Collections", "Nav_ListView");
        await (await GalleryNav.BringAsync(app, "ListViewPage_Main")).SelectAsync(0);
        await (await GalleryNav.BringAsync(app, "ListViewPage_SelectionMode")).SelectAsync(1);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// TreeView expand/select.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - TreeViewPage_Main
    ///
    /// Steps:
    /// - Navigate Nav_TreeView
    /// - ExpectVisible + Screenshot
    ///
    /// Expected:
    /// - Tree visible
    /// </remarks>
    [Fact]
    public async Task TreeView_Visible_AndScreenshot()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Collections", "Nav_TreeView");
        await app.GetByAutomationId("TreeViewPage_Main").ExpectVisibleAsync(true);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// TreeList page is a Gallery stub ("To do.").
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents TreeList stub
    /// </remarks>
    [Fact(Skip = "TreeList: Gallery DEBUG stub (\"To do.\"); not deep-automated")]
    public void TreeList_Stub_Skipped() { }

    private async Task<GraftSession> ReadyAsync()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        return app;
    }
}
