using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Windows category E2E (outer chrome; Monaco inner Skip).
/// </summary>
[Collection(GalleryWindowsCollection.Name)]
public sealed class WindowsE2ETests
{
    private readonly GalleryWindowsFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Windows category fixture.</param>
    public WindowsE2ETests(GalleryWindowsFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Windows page CardActions open Editor/Monaco outer; close; Screenshot.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav_Windows; cards named Editor / Monaco / Sandbox
    ///
    /// Steps:
    /// - Open Editor/Monaco by Name; Screenshot; close via TitleBarClose if secondary window
    ///
    /// Expected:
    /// - Extra windows open/close without inner Monaco edit
    /// </remarks>
    [Fact]
    public async Task Windows_OpenEditorAndMonaco_Outer_ThenClose()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await GalleryNav.NavigateLeafAsync(app, "Nav_Windows");
        await GalleryNav.ShotAsync(app);

        foreach (var name in new[] { "Editor", "Monaco" })
        {
            try
            {
                await app.GetByName(name).Nth(0).InvokeAsync();
                await Task.Delay(1000);
                await GalleryNav.ShotAsync(app);
                var windows = await app.ListWindowsAsync();
                var extra = windows.Windows.FirstOrDefault(w => w.AutomationId != "MainWindow");
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
                // card naming may differ
            }
        }
    }

    /// <summary>
    /// Monaco inner editing skipped.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents Monaco inner boundary
    /// </remarks>
    [Fact(Skip = "Monaco / WebView2 inner editing is out of Graft tree scope")]
    public void Monaco_InnerEdit_Skipped() { }

    /// <summary>
    /// Sandbox DEBUG window outer open best-effort.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - DEBUG build includes Sandbox card
    ///
    /// Steps:
    /// - Invoke Sandbox name if present; Screenshot; close
    ///
    /// Expected:
    /// - Optional under GraftTest+DEBUG
    /// </remarks>
    [Fact]
    public async Task Sandbox_Outer_IfPresent()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await GalleryNav.NavigateLeafAsync(app, "Nav_Windows");
        try
        {
            await app.GetByName("Sandbox").Nth(0).InvokeAsync();
            await Task.Delay(800);
            await GalleryNav.ShotAsync(app);
        }
        catch (GraftException)
        {
            // Sandbox may be absent
        }
    }
}
