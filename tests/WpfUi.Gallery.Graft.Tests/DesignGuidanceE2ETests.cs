using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Design guidance category E2E (Typography / Icons / Colors).
/// </summary>
[Collection(GalleryDesignCollection.Name)]
public sealed class DesignGuidanceE2ETests
{
    private readonly GalleryDesignFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignGuidanceE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Design category fixture.</param>
    public DesignGuidanceE2ETests(GalleryDesignFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Typography page is reachable and screenshots.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Shared design category session
    ///
    /// Steps:
    /// - Navigate Nav_DesignGuidance → Nav_Typography
    /// - Screenshot
    ///
    /// Expected:
    /// - Page loads; PNG non-empty
    /// </remarks>
    [Fact]
    public async Task Typography_Reachable_AndScreenshot()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        await GalleryNav.NavigateAsync(app, "Nav_DesignGuidance", "Nav_Typography");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Icons page reachable with hover/screenshot.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Shared design session
    ///
    /// Steps:
    /// - Navigate Nav_Icons
    /// - Screenshot
    ///
    /// Expected:
    /// - Page loads; PNG non-empty
    /// </remarks>
    [Fact]
    public async Task Icons_Reachable_AndScreenshot()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        await GalleryNav.NavigateAsync(app, "Nav_DesignGuidance", "Nav_Icons");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Colors page reachable and screenshots.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Shared design session
    ///
    /// Steps:
    /// - Navigate Nav_Colors
    /// - Screenshot
    ///
    /// Expected:
    /// - Page loads; PNG non-empty
    /// </remarks>
    [Fact]
    public async Task Colors_Reachable_AndScreenshot()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        await GalleryNav.NavigateAsync(app, "Nav_DesignGuidance", "Nav_Colors");
        await GalleryNav.ShotAsync(app);
    }
}
