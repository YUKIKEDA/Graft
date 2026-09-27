using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Basic Input category deep E2E.
/// </summary>
[Collection(GalleryBasicInputCollection.Name)]
public sealed class BasicInputE2ETests
{
    private readonly GalleryBasicInputFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicInputE2ETests"/> class.
    /// </summary>
    /// <param name="fixture">Basic Input fixture.</param>
    public BasicInputE2ETests(GalleryBasicInputFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Button primary Invoke and Disable checkbox Toggle.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ButtonPage_Primary / ButtonPage_DisableCheck AutomationIds
    ///
    /// Steps:
    /// - Navigate Nav_BasicInput → Nav_Button
    /// - Invoke ButtonPage_Primary
    /// - Toggle ButtonPage_DisableCheck
    /// - Screenshot
    ///
    /// Expected:
    /// - Controls resolve and actions succeed
    /// </remarks>
    [Fact]
    public async Task Button_InvokePrimary_AndToggleDisable()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_Button");
        await (await GalleryNav.BringAsync(app, "ButtonPage_Primary")).InvokeAsync();
        await (await GalleryNav.BringAsync(app, "ButtonPage_DisableCheck")).ToggleAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// CheckBox two-state and three-state Toggle.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - CheckBoxPage_TwoState / CheckBoxPage_ThreeState
    ///
    /// Steps:
    /// - Navigate Nav_CheckBox
    /// - Toggle both checkboxes
    /// - Screenshot
    ///
    /// Expected:
    /// - Toggles succeed
    /// </remarks>
    [Fact]
    public async Task CheckBox_ToggleTwoStateAndThreeState()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_CheckBox");
        await (await GalleryNav.BringAsync(app, "CheckBoxPage_TwoState")).ToggleAsync();
        await (await GalleryNav.BringAsync(app, "CheckBoxPage_ThreeState")).ToggleAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// ToggleButton and ToggleSwitch main controls.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ToggleButtonPage_Main / ToggleSwitchPage_Main
    ///
    /// Steps:
    /// - Navigate each page and Toggle
    /// - Screenshot on ToggleSwitch
    ///
    /// Expected:
    /// - Both toggles succeed
    /// </remarks>
    [Fact]
    public async Task ToggleButtonAndSwitch_ToggleMain()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_ToggleButton");
        await (await GalleryNav.BringAsync(app, "ToggleButtonPage_Main")).ToggleAsync();
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_ToggleSwitch");
        await (await GalleryNav.BringAsync(app, "ToggleSwitchPage_Main")).ToggleAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// ComboBox Select and RadioButton select.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ComboBoxPage_Colors / RadioButtonPage_Option1
    ///
    /// Steps:
    /// - Navigate ComboBox → SelectAsync(2)
    /// - Navigate RadioButton → Toggle Option1
    /// - Screenshot
    ///
    /// Expected:
    /// - Selection/toggle succeed
    /// </remarks>
    [Fact]
    public async Task ComboBoxAndRadio_Select()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_ComboBox");
        await (await GalleryNav.BringAsync(app, "ComboBoxPage_Colors")).SelectAsync(2);
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_RadioButton");
        await (await GalleryNav.BringAsync(app, "RadioButtonPage_Option1")).ToggleAsync();
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Slider SetValue.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SliderPage_Main
    ///
    /// Steps:
    /// - Navigate Nav_Slider
    /// - SetValueAsync "42"
    /// - Screenshot
    ///
    /// Expected:
    /// - Value set succeeds
    /// </remarks>
    [Fact]
    public async Task Slider_SetValue()
    {
        var app = await ReadyAsync();
        await GalleryNav.NavigateAsync(app, "Nav_BasicInput", "Nav_Slider");
        await (await GalleryNav.BringAsync(app, "SliderPage_Main")).SetValueAsync("42");
        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// Anchor / DropDown / Rating / ThumbRate / Split pages reachability.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nav leaves under Basic Input
    ///
    /// Steps:
    /// - Visit Anchor, DropDownButton, Rating, ThumbRate, SplitButton hubs
    /// - Screenshot last page
    ///
    /// Expected:
    /// - Each leaf navigates without error
    /// </remarks>
    [Fact]
    public async Task OtherBasicInputLeaves_AreReachable()
    {
        var app = await ReadyAsync();
        foreach (var leaf in new[] { "Nav_Anchor", "Nav_DropDownButton", "Nav_HyperlinkButton", "Nav_Rating", "Nav_ThumbRate", "Nav_SplitButton" })
        {
            await GalleryNav.NavigateAsync(app, "Nav_BasicInput", leaf);
        }

        await GalleryNav.ShotAsync(app);
    }

    /// <summary>
    /// External hyperlink navigation is a product non-goal Skip.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - N/A
    ///
    /// Steps:
    /// - Skipped
    ///
    /// Expected:
    /// - Skip reason documents external browser navigation
    /// </remarks>
    [Fact(Skip = "External hyperlink navigation: out of process browser; product non-goal")]
    public void Hyperlink_ExternalNavigation_Skipped() { }

    private async Task<GraftSession> ReadyAsync()
    {
        GalleryNav.EnsureReady(_fixture);
        var app = _fixture.Session;
        GalleryNav.ApplyDefaultWaits(app);
        await app.WaitForWindowAsync(automationId: "MainWindow");
        return app;
    }
}
