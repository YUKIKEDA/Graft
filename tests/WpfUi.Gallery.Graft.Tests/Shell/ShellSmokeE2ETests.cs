using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Batch 1 smoke: Gallery Launch + shell AutomationIds + Timeline Always.
/// </summary>
[Collection(GalleryShellCollection.Name)]
public sealed class ShellSmokeE2ETests
{
    private readonly GalleryShellFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellSmokeE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Shared Shell category fixture.</param>
    public ShellSmokeE2ETests(GalleryShellFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Launched Gallery exposes MainWindow, AutoSuggest, and NavigationView; window screenshot is captured.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - tests/wpfui Gallery with local Graft Instrumentation patch
    /// - Shared Shell collection session (Timeline Always)
    ///
    /// Steps:
    /// - WaitForWindowAsync(MainWindow)
    /// - GetByAutomationId NavigationAutoSuggestBox / Gallery_NavigationView
    /// - session.ScreenshotAsync()
    ///
    /// Expected:
    /// - Elements resolve; PNG non-empty; fixture Dispose asserts Timeline index.html + frames
    /// </remarks>
    [Fact]
    public async Task Launch_ShellAutomationIds_AreReachable_AndScreenshotCaptured()
    {
        EnsureGalleryReady();

        var app = _fixture.Session;
        await app.WaitForWindowAsync(automationId: "MainWindow");

        await app.GetByAutomationId("NavigationAutoSuggestBox").ExpectVisibleAsync(true);
        await app.GetByAutomationId("Gallery_NavigationView").ExpectVisibleAsync(true);

        var shot = await app.ScreenshotAsync();
        Assert.NotNull(shot.PngBytes);
        Assert.NotEmpty(shot.PngBytes);
        Assert.True(shot.Width > 0);
        Assert.True(shot.Height > 0);
    }

    /// <summary>
    /// Footer Settings nav opens the Settings page (About section visible).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Shared Shell session
    /// - Gallery_SettingsNavItem AutomationId on footer item
    ///
    /// Steps:
    /// - DoubleClickAsync Gallery_SettingsNavItem (SendInput; Invoke alone may not navigate Frame)
    /// - ExpectVisible About + SettingsPage_ThemeCombo
    /// - Screenshot
    ///
    /// Expected:
    /// - Settings content reachable; PNG captured for Timeline
    /// </remarks>
    [Fact]
    public async Task Navigate_Settings_ThemeCombo_IsReachable()
    {
        EnsureGalleryReady();

        var app = _fixture.Session;
        app.WaitOptions = new WaitOptions { ActionTimeout = TimeSpan.FromSeconds(30), ExpectTimeout = TimeSpan.FromSeconds(30) };
        await app.WaitForWindowAsync(automationId: "MainWindow");

        // Prefer footer Settings Id; Invoke alone may not switch Frame — use DoubleClick as SendInput path.
        await app.GetByAutomationId("Gallery_SettingsNavItem").DoubleClickAsync();
        await app.GetByName("About").ExpectVisibleAsync(true);
        await app.GetByAutomationId("SettingsPage_ThemeCombo").ExpectVisibleAsync(true);

        var shot = await app.ScreenshotAsync();
        Assert.NotEmpty(shot.PngBytes);
    }

    private void EnsureGalleryReady()
    {
        if (!_fixture.IsReady)
        {
            throw new InvalidOperationException("Gallery was not launched. Ensure tests/wpfui is cloned and Graft-patched.");
        }
    }
}
