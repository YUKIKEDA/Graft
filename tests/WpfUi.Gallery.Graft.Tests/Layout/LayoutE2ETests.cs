using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Layout category E2E (Debug-only Gallery pages).
/// </summary>
[Collection(GalleryLayoutCollection.Name)]
public sealed class LayoutE2ETests
{
    private readonly GalleryLayoutFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Layout fixture.</param>
    public LayoutE2ETests(GalleryLayoutFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Expander page toggle/open.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - GraftTest Debug build exposes Nav_Layout / ExpanderPage_Main
    ///
    /// Steps:
    /// - Navigate Expander
    /// - Expand/toggle ExpanderPage_Main
    /// - Screenshot
    ///
    /// Expected:
    /// - Expander operable
    /// </remarks>
    [Fact]
    public async Task Expander_Toggle_AndScreenshot()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Layout", "Nav_Expander");
        var expander = await GalleryNav.BringAsync(app, "ExpanderPage_Main");
        try
        {
            await expander.ToggleAsync();
        }
        catch
        {
            await expander.InvokeAsync();
        }

        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// CardControl / CardAction hub pages reachable.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav_CardControl / Nav_CardAction under Layout
    ///
    /// Steps:
    /// - Navigate both leaves
    /// - Screenshot
    ///
    /// Expected:
    /// - Pages load
    /// </remarks>
    [Fact]
    public async Task CardPages_AreReachable()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Layout", "Nav_CardControl");
        await GalleryNav.NavigateAsync(app, "Nav_Layout", "Nav_CardAction");
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
