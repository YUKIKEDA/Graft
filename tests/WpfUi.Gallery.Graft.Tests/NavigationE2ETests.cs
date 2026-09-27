using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Navigation category E2E.
/// </summary>
[Collection(GalleryNavigationCollection.Name)]
public sealed class NavigationE2ETests
{
    private readonly GalleryNavigationFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Navigation fixture.</param>
    public NavigationE2ETests(GalleryNavigationFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// BreadcrumbBar and in-page NavigationView sample reachable.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav_BreadcrumbBar / Nav_NavigationViewPage
    ///
    /// Steps:
    /// - Navigate both
    /// - Screenshot
    ///
    /// Expected:
    /// - Pages load
    /// </remarks>
    [Fact]
    public async Task BreadcrumbAndNavigationViewPage_Reachable()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Navigation", "Nav_BreadcrumbBar");
        await GalleryNav.NavigateAsync(app, "Nav_Navigation", "Nav_NavigationViewPage");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Menu SelectMenuAsync File → New.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - MenuPage_Main
    ///
    /// Steps:
    /// - Navigate Nav_Menu
    /// - SelectMenuAsync("File|New") or "File/New"
    /// - Screenshot
    ///
    /// Expected:
    /// - Menu path executes
    /// </remarks>
    [Fact]
    public async Task Menu_SelectMenu_FileNew()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Navigation", "Nav_Menu");
        await (await GalleryNav.BringAsync(app, "MenuPage_Main")).SelectMenuAsync("MenuPage_File/MenuPage_File_New");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Multilevel start navigate.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - MultilevelPage_Start
    ///
    /// Steps:
    /// - Navigate Nav_Multilevel
    /// - Invoke MultilevelPage_Start
    /// - Screenshot
    ///
    /// Expected:
    /// - Sample page navigation starts
    /// </remarks>
    [Fact]
    public async Task Multilevel_Start_Invokes()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Navigation", "Nav_Multilevel");
        await (await GalleryNav.BringAsync(app, "MultilevelPage_Start")).InvokeAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// TabControl select.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - TabControlPage_Main
    ///
    /// Steps:
    /// - Navigate Nav_TabControl
    /// - SelectAsync(0)
    /// - Screenshot
    ///
    /// Expected:
    /// - Tab switch succeeds
    /// </remarks>
    [Fact]
    public async Task TabControl_SelectTab()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Navigation", "Nav_TabControl");
        await (await GalleryNav.BringAsync(app, "TabControlPage_Main")).SelectAsync(0);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// TabView is not in main nav.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents missing nav entry
    /// </remarks>
    [Fact(Skip = "TabView: not in main NavigationView menu; AllControls-only / Skip")]
    public void TabView_NotInNav_Skipped() { }

    private async Task<GraftSession> ReadyAsync()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        return app;
    }
}
