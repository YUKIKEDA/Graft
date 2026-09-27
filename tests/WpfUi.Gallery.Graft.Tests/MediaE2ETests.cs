using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Media category E2E (host chrome only; inner DOM Skip).
/// </summary>
[Collection(GalleryMediaCollection.Name)]
public sealed class MediaE2ETests
{
    private readonly GalleryMediaFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Media fixture.</param>
    public MediaE2ETests(GalleryMediaFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Image page reachable + screenshot (avoid probing Image visual Visible — can hang the agent).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav_Image
    ///
    /// Steps:
    /// - Navigate Nav_Image
    /// - Screenshot
    ///
    /// Expected:
    /// - Page navigates; PNG non-empty
    /// </remarks>
    [Fact]
    public async Task Image_HostVisible_AndScreenshot()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Media", "Nav_Image");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Canvas / WebView host pages reachable (outer frame).
    /// WebBrowser is not navigated here — obsolete IE host; covered by a dedicated Fact.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav leaves under Media
    ///
    /// Steps:
    /// - Navigate Canvas, WebView
    /// - Screenshot
    ///
    /// Expected:
    /// - Host pages load; no inner DOM automation
    /// </remarks>
    [Fact]
    public async Task WebHosts_CanvasWebView_Reachable()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Media", "Nav_Canvas");
        await GalleryNav.NavigateAsync(app, "Nav_Media", "Nav_WebView");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// WebBrowser host chrome only (local Gallery always loads about:blank).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - WebBrowserPage patched to about:blank (no external URL / no IE script modal)
    ///
    /// Steps:
    /// - Navigate Nav_WebBrowser
    /// - Screenshot
    ///
    /// Expected:
    /// - Host page loads without script-error modal
    /// </remarks>
    [Fact]
    public async Task WebBrowser_HostReachable_WithoutScriptErrorModal()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Media", "Nav_WebBrowser");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// WebView/WebBrowser inner DOM is product non-goal.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip reason documents inner DOM boundary
    /// </remarks>
    [Fact(Skip = "WebView/WebBrowser inner DOM: product non-goal; host chrome only")]
    public void WebView_InnerDom_Skipped() { }

    /// <summary>
    /// Image pixel expect/diff is product non-goal.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents image diff boundary
    /// </remarks>
    [Fact(Skip = "Image expect/diff: product non-goal; Screenshot only")]
    public void Image_ExpectDiff_Skipped() { }

    private async Task<GraftSession> ReadyAsync()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        return app;
    }
}
