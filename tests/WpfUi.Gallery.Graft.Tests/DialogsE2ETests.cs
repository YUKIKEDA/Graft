using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Dialogs &amp; flyouts category E2E.
/// </summary>
[Collection(GalleryDialogsCollection.Name)]
public sealed class DialogsE2ETests
{
    private readonly GalleryDialogsFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogsE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Dialogs fixture.</param>
    public DialogsE2ETests(GalleryDialogsFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Snackbar show trigger.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SnackbarPage_Show
    ///
    /// Steps:
    /// - Navigate Nav_Snackbar
    /// - Invoke SnackbarPage_Show
    /// - Screenshot
    ///
    /// Expected:
    /// - Invoke succeeds
    /// </remarks>
    [Fact]
    public async Task Snackbar_Show_Invokes()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Dialogs", "Nav_Snackbar");
        await (await GalleryNav.BringAsync(app, "SnackbarPage_Show")).InvokeAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// ContentDialog Show then dismiss (runs last so a stuck overlay cannot block Flyout/Snackbar).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ContentDialogPage_Show; dialog buttons named Cancel/Save
    ///
    /// Steps:
    /// - Navigate Nav_ContentDialog
    /// - Invoke Show
    /// - Wait + dismiss Cancel/Save
    /// - Screenshot
    ///
    /// Expected:
    /// - Dialog opens; dismiss attempted
    /// </remarks>
    [Fact]
    public async Task Zzz_ContentDialog_Show_AndDismissIfPossible()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Dialogs", "Nav_ContentDialog");
        await (await GalleryNav.BringAsync(app, "ContentDialogPage_Show")).InvokeAsync();
        await GalleryNav.DismissContentDialogBestEffortAsync(app);
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Flyout open.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - FlyoutPage_Open
    ///
    /// Steps:
    /// - Navigate Nav_Flyout
    /// - Invoke Open
    /// - Screenshot
    ///
    /// Expected:
    /// - Flyout host opens
    /// </remarks>
    [Fact]
    public async Task Flyout_Open_Invokes()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Dialogs", "Nav_Flyout");
        await (await GalleryNav.BringAsync(app, "FlyoutPage_Open")).InvokeAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Standard MessageBox via ArmMessageBox + InvokeOpeningWindow.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - MessageBoxPage_Standard uses System.Windows.MessageBox.Show
    ///
    /// Steps:
    /// - Navigate Nav_MessageBox
    /// - ArmMessageBoxAsync("OK")
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false)
    /// - Screenshot
    ///
    /// Expected:
    /// - Arm path completes without hang
    /// </remarks>
    [Fact]
    public async Task MessageBox_Standard_ArmOk()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_Dialogs", "Nav_MessageBox");
        await app.ArmMessageBoxAsync("OK");
        _ = await (await GalleryNav.BringAsync(app, "MessageBoxPage_Standard")).InvokeOpeningWindowAsync(waitForNewWindow: false);
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
