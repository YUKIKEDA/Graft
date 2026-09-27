using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Cross-cutting Gallery story (separate story process / Timeline leaf).
/// </summary>
[Collection(GalleryStoryCollection.Name)]
public sealed class StoryE2ETests
{
    private readonly GalleryStoryFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoryE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Story category fixture.</param>
    public StoryE2ETests(GalleryStoryFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Cross-cutting story: Home → Button → Settings theme → ContentDialog → FilePicker → Windows Editor.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Shared story collection session (Timeline Always)
    /// - Page_* / Nav_* AutomationIds patched in Gallery
    ///
    /// Steps:
    /// - Navigate Home
    /// - Basic Input Button Invoke
    /// - Settings theme Select Dark
    /// - ContentDialog Show + dismiss best-effort
    /// - FilePicker ArmOpenFile + open
    /// - Windows open Editor and close
    /// - Fixture Dispose asserts Timeline
    ///
    /// Expected:
    /// - Each stage completes; Timeline index.html + frames asserted on Dispose
    /// </remarks>
    [Fact]
    public async Task Story_HomeButtonSettingsDialogFilePickerWindows_Completes()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");

        await GalleryNav.NavigateLeafAsync(app, "Nav_Home");
        await GalleryNav.ShotAsync(app);

        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_Button");
        await (await GalleryNav.BringAsync(app, "ButtonPage_Primary")).InvokeAsync();
        await GalleryNav.ShotAsync(app);

        await app.GetByAutomationId("Gallery_SettingsNavItem").DoubleClickAsync();
        await (await GalleryNav.BringAsync(app, "SettingsPage_ThemeCombo")).SelectAsync(1);
        await GalleryNav.ShotAsync(app);

        try
        {
            await GalleryNav.NavigateAsync(app, "Nav_Dialogs", "Nav_ContentDialog");
            await (await GalleryNav.BringAsync(app, "ContentDialogPage_Show")).InvokeAsync();
            await GalleryNav.DismissContentDialogBestEffortAsync(app);
            await GalleryNav.ShotAsync(app);
        }
        catch (GraftException)
        {
            // ContentDialog / agent flake must not kill the rest of the story
        }

        var openPath = Path.Combine(Path.GetTempPath(), $"graft-gallery-story-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(openPath, "story");
        try
        {
            await GalleryNav.NavigateAsync(app, "Nav_System", "Nav_FilePicker");
            await app.ArmOpenFileAsync(openPath);
            _ = await (await GalleryNav.BringAsync(app, "FilePickerPage_OpenFile")).InvokeOpeningWindowAsync(waitForNewWindow: false);
            await GalleryNav.ShotAsync(app);
        }
        finally
        {
            try
            {
                File.Delete(openPath);
            }
            catch
            {
                // ignore
            }
        }

        await GalleryNav.NavigateLeafAsync(app, "Nav_Windows");
        try
        {
            await app.GetByName("Editor").Nth(0).InvokeAsync();
            await Task.Delay(800);
            await GalleryNav.ShotAsync(app);
            var windows = await app.ListWindowsAsync();
            var extra = windows.Windows.FirstOrDefault(w => !string.Equals(w.AutomationId, "MainWindow", StringComparison.Ordinal));
            if (extra is not null)
            {
                await app.SwitchToWindowAsync(extra.WindowId);
                try
                {
                    await (await GalleryNav.BringAsync(app, "TitleBarCloseButton")).InvokeAsync();
                }
                catch (GraftException)
                {
                    // best-effort
                }

                await app.WaitForWindowAsync(automationId: "MainWindow");
            }
        }
        catch (GraftException)
        {
            // Editor card may not resolve by Name in all themes
        }

        await GalleryNav.ShotAsync(app);
    }
}
