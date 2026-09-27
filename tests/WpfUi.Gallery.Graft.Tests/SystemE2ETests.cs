using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// System category E2E (Clipboard UI + FilePicker Arm*).
/// </summary>
[Collection(GallerySystemCollection.Name)]
public sealed class SystemE2ETests
{
    private readonly GallerySystemFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">System category fixture.</param>
    public SystemE2ETests(GallerySystemFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Clipboard UI copy/paste via Gallery buttons (not V06 API).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ClipboardPage_Input / Copy / Paste
    ///
    /// Steps:
    /// - SetValue input; Copy; Paste; Screenshot
    ///
    /// Expected:
    /// - Gallery UI path works
    /// </remarks>
    [Fact]
    public async Task Clipboard_CopyPaste_ViaGalleryUi()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await GalleryNav.NavigateAsync(app, "Nav_System", "Nav_Clipboard");
        await (await GalleryNav.BringAsync(app, "ClipboardPage_Input")).SetValueAsync("gallery-clipboard");
        await (await GalleryNav.BringAsync(app, "ClipboardPage_Copy")).InvokeAsync();
        await (await GalleryNav.BringAsync(app, "ClipboardPage_Paste")).InvokeAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// FilePicker ArmOpenFile then Open a file.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - FilePickerPage_OpenFile; temp fixture file
    ///
    /// Steps:
    /// - ArmOpenFile; InvokeOpeningWindow; Screenshot
    ///
    /// Expected:
    /// - Arm path completes
    /// </remarks>
    [Fact]
    public async Task FilePicker_ArmOpenFile()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        var path = Path.Combine(Path.GetTempPath(), $"graft-gallery-open-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "open");
        try
        {
            await GalleryNav.NavigateAsync(app, "Nav_System", "Nav_FilePicker");
            await app.ArmOpenFileAsync(path);
            _ = await (await GalleryNav.BringAsync(app, "FilePickerPage_OpenFile")).InvokeOpeningWindowAsync(waitForNewWindow: false);
            await GalleryNav.ShotAsync(app);
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// FilePicker ArmSaveFile.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - FilePickerPage_SaveFile
    ///
    /// Steps:
    /// - ArmSaveFile temp path; InvokeOpeningWindow; Screenshot
    ///
    /// Expected:
    /// - Arm path completes
    /// </remarks>
    [Fact]
    public async Task FilePicker_ArmSaveFile()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        var path = Path.Combine(Path.GetTempPath(), $"graft-gallery-save-{Guid.NewGuid():N}.txt");
        try
        {
            await GalleryNav.NavigateAsync(app, "Nav_System", "Nav_FilePicker");
            await app.ArmSaveFileAsync(path);
            _ = await (await GalleryNav.BringAsync(app, "FilePickerPage_SaveFile")).InvokeOpeningWindowAsync(waitForNewWindow: false);
            await GalleryNav.ShotAsync(app);
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// FilePicker ArmOpenFolder.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - FilePickerPage_OpenFolder
    ///
    /// Steps:
    /// - ArmOpenFolder temp dir; InvokeOpeningWindow; Screenshot
    ///
    /// Expected:
    /// - Arm path completes
    /// </remarks>
    [Fact]
    public async Task FilePicker_ArmOpenFolder()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        var dir = Path.Combine(Path.GetTempPath(), $"graft-gallery-folder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            await GalleryNav.NavigateAsync(app, "Nav_System", "Nav_FilePicker");
            await app.ArmOpenFolderAsync(dir);
            _ = await (await GalleryNav.BringAsync(app, "FilePickerPage_OpenFolder")).InvokeOpeningWindowAsync(waitForNewWindow: false);
            await GalleryNav.ShotAsync(app);
        }
        finally
        {
            GalleryLaunch.TryDelete(dir);
        }
    }

    /// <summary>
    /// V06 clipboard paste API skipped.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents V06 gap
    /// </remarks>
    [Fact(Skip = "V06 clipboard paste API is optional/unimplemented in Graft (competitive-gap); Gallery UI path covered above")]
    public void Clipboard_V06_Api_Skipped() { }

    /// <summary>
    /// Real OS common dialog UIA skipped.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip documents W05 boundary
    /// </remarks>
    [Fact(Skip = "W05 real OS common dialog UIA is product non-goal; use Arm* seams instead")]
    public void FilePicker_RealOsDialogUia_Skipped() { }
}
