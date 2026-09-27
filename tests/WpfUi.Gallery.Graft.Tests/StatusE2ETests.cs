using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Status &amp; info category E2E.
/// </summary>
[Collection(GalleryStatusCollection.Name)]
public sealed class StatusE2ETests
{
    private readonly GalleryStatusFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Status fixture.</param>
    public StatusE2ETests(GalleryStatusFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// InfoBadge and InfoBar toggle.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - InfoBarPage_Toggle
    ///
    /// Steps:
    /// - Navigate InfoBadge then InfoBar
    /// - Toggle InfoBarPage_Toggle
    /// - Screenshot
    ///
    /// Expected:
    /// - Pages operable
    /// </remarks>
    [Fact]
    public async Task InfoBadgeAndInfoBar_Toggle()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Status", "Nav_InfoBadge");
        await GalleryNav.NavigateAsync(app, "Nav_Status", "Nav_InfoBar");
        await (await GalleryNav.BringAsync(app, "InfoBarPage_Toggle")).ToggleAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// ProgressBar / ProgressRing visible.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ProgressBarPage_Main
    ///
    /// Steps:
    /// - Navigate ProgressBar + ProgressRing
    /// - ExpectVisible ProgressBar
    /// - Screenshot
    ///
    /// Expected:
    /// - Progress hosts visible
    /// </remarks>
    [Fact]
    public async Task ProgressBarAndRing_Visible()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Status", "Nav_ProgressBar");
        await app.GetByAutomationId("ProgressBarPage_Main").ExpectVisibleAsync(true);
        await GalleryNav.NavigateAsync(app, "Nav_Status", "Nav_ProgressRing");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// ToolTip Hover on host.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ToolTipPage_Host
    ///
    /// Steps:
    /// - Navigate Nav_ToolTip
    /// - HoverAsync host
    /// - Screenshot
    ///
    /// Expected:
    /// - Hover succeeds (tip may be popup)
    /// </remarks>
    [Fact]
    public async Task ToolTip_HoverHost()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Status", "Nav_ToolTip");
        await (await GalleryNav.BringAsync(app, "ToolTipPage_Host")).HoverAsync();
        await Task.Delay(400);
        await GalleryNav.ShotAsync(app);
    }

    private async Task<GraftSession> ReadyAsync()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        return app;
    }
}
