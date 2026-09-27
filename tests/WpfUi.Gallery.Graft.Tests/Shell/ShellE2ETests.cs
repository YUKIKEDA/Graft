using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Batch 2 Shell: title, TitleBar, theme, nav expand (shared Shell process).
/// </summary>
[Collection(GalleryShellCollection.Name)]
public sealed class ShellE2ETests
{
    private readonly GalleryShellFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Shared Shell fixture.</param>
    public ShellE2ETests(GalleryShellFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Main window title contains WPF UI Gallery via ListWindows.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Shared Shell session launched
    ///
    /// Steps:
    /// - WaitForWindowAsync(MainWindow)
    /// - ListWindowsAsync
    ///
    /// Expected:
    /// - At least one window Title contains "WPF UI Gallery"
    /// </remarks>
    [Fact]
    public async Task WindowTitle_ContainsGalleryBrand()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");

        var listed = await app.ListWindowsAsync();
        Assert.Contains(listed.Windows, w => w.Title.Contains("WPF UI Gallery", StringComparison.OrdinalIgnoreCase));
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// TitleBar Maximize invoke (Minimize moved off shared session — it disconnects the agent pipe).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - TitleBarMaximizeButton AutomationId from Wpf.Ui TitleBar
    ///
    /// Steps:
    /// - Invoke TitleBarMaximizeButton
    /// - Screenshot
    ///
    /// Expected:
    /// - Action succeeds; PNG captured
    /// </remarks>
    [Fact]
    public async Task TitleBar_Maximize_Succeeds()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");

        await (await GalleryNav.BringAsync(app, "TitleBarMaximizeButton")).InvokeAsync();
        await Task.Delay(400);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// TitleBar Minimize is covered separately (shared session unsafe).
    /// </summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip
    /// Expected: Skipped — Minimize drops interactive session reliability
    /// </remarks>
    [Fact(Skip = "TitleBar Minimize on shared Shell session loses named-pipe/agent focus; use Maximize or dedicated process")]
    public void TitleBar_Minimize_SkippedOnSharedSession() { }

    /// <summary>
    /// Settings theme SelectAsync Dark then Light with screenshots.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Gallery_SettingsNavItem + SettingsPage_ThemeCombo (Light=0, Dark=1)
    ///
    /// Steps:
    /// - DoubleClick Settings nav
    /// - SelectAsync(1) Dark + Shot
    /// - SelectAsync(0) Light + Shot
    ///
    /// Expected:
    /// - Theme combo operable; screenshots non-empty
    /// </remarks>
    [Fact]
    public async Task Settings_Theme_SelectDarkThenLight()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");

        await app.GetByAutomationId("Gallery_SettingsNavItem").DoubleClickAsync();
        await (await GalleryNav.BringAsync(app, "SettingsPage_ThemeCombo")).SelectAsync(1);
        await GalleryNav.ShotAsync(app);
        await (await GalleryNav.BringAsync(app, "SettingsPage_ThemeCombo")).SelectAsync(0);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Expands Basic Input parent navigation item.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav_BasicInput AutomationId on parent NavigationViewItem
    ///
    /// Steps:
    /// - ExpandAsync Nav_BasicInput (AutomationPeer; tree expanded may be n/a)
    /// - ExpectVisible Nav_Button
    ///
    /// Expected:
    /// - Leaf Button nav item becomes reachable
    /// </remarks>
    [Fact]
    public async Task Nav_ExpandBasicInput_ExposesButtonLeaf()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");

        await app.GetByAutomationId("Nav_BasicInput").ExpandAsync();
        await app.GetByAutomationId("Nav_Button").ExpectVisibleAsync(true);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// AutoSuggest path is flaky (host not SetValue-focusable); Settings footer DoubleClick covers reachability.
    /// </summary>
    /// <remarks>
    /// Preconditions: N/A
    /// Steps: Skip — use Gallery_SettingsNavItem DoubleClick instead (ShellSmoke / Theme tests)
    /// Expected: Skipped with reason
    /// </remarks>
    [Fact(Skip = "NavigationAutoSuggestBox host is not SetValue-focusable; Settings reach covered via Gallery_SettingsNavItem")]
    public void AutoSuggest_Settings_Skipped_UseFooterNav() { }

    /// <summary>
    /// Tray menu automation is not first-class in Graft; skipped with gap reason.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A (product non-goal / gap)
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents competitive-gap W12-adjacent tray automation
    /// </remarks>
    [Fact(Skip = "Tray: Graft tray automation not first-class; see competitive-gap W12-adjacent")]
    public void TrayMenu_Skipped_NoFirstClassApi() { }
}
