using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Text category E2E.
/// </summary>
[Collection(GalleryTextCollection.Name)]
public sealed class TextE2ETests
{
    private readonly GalleryTextFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Text category fixture.</param>
    public TextE2ETests(GalleryTextFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// TextBox SetValue, Ctrl+A Delete, empty Expect, Focus.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - TextBoxPage_Main
    ///
    /// Steps:
    /// - Navigate Nav_TextBox
    /// - SetValue; Press Control+A; Press Delete; Expect empty; ExpectFocused
    ///
    /// Expected:
    /// - Empty after clear; focused
    /// </remarks>
    [Fact]
    public async Task TextBox_SetValue_SelectAll_Delete_Empty()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await GalleryNav.NavigateAsync(app, "Nav_Text", "Nav_TextBox");
        var box = await GalleryNav.BringAsync(app, "TextBoxPage_Main");
        await box.SetValueAsync("graft-gallery");
        await box.PressAsync("Control+A");
        await box.PressAsync("Delete");
        await box.ExpectNameAsync(string.Empty);
        await box.ExpectFocusedAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// PasswordBox Set only (no Get).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - PasswordBoxPage_Main
    ///
    /// Steps:
    /// - SetValue secret; Screenshot
    ///
    /// Expected:
    /// - Set succeeds without reading password
    /// </remarks>
    [Fact]
    public async Task PasswordBox_SetOnly()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await GalleryNav.NavigateAsync(app, "Nav_Text", "Nav_PasswordBox");
        await (await GalleryNav.BringAsync(app, "PasswordBoxPage_Main")).SetValueAsync("secret");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// NumberBox SetValue.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - NumberBoxPage_Main
    ///
    /// Steps:
    /// - SetValue 25; Screenshot
    ///
    /// Expected:
    /// - Set succeeds
    /// </remarks>
    [Fact]
    public async Task NumberBox_SetValue()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await GalleryNav.NavigateAsync(app, "Nav_Text", "Nav_NumberBox");
        await (await GalleryNav.BringAsync(app, "NumberBoxPage_Main")).SetValueAsync("25");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// AutoSuggest / RichText / Label / TextBlock pages reachable.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav leaves under Text
    ///
    /// Steps:
    /// - Navigate each leaf; Screenshot
    ///
    /// Expected:
    /// - Pages load
    /// </remarks>
    [Fact]
    public async Task AutoSuggest_RichText_Label_TextBlock_Reach()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        foreach (var leaf in new[] { "Nav_AutoSuggestBox", "Nav_RichTextBox", "Nav_Label", "Nav_TextBlock" })
        {
            await GalleryNav.NavigateAsync(app, "Nav_Text", leaf);
            await GalleryNav.ShotAsync(app);
        }
    }
}
