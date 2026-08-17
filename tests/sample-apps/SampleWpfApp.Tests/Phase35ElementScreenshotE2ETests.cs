using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 35 element-clip screenshot acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase35ElementScreenshotE2ETests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// SampleButton element clip is a PNG smaller than the target window screenshot.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Launch sample
    /// - WaitFor SampleButton (window layout)
    /// - session.ScreenshotAsync() / GetByAutomationId(SampleButton).ScreenshotAsync()
    /// - SaveAsync to Artifacts
    ///
    /// Expected:
    /// - Element clip PNG signature, width/height &gt; 0, smaller than the window shot
    /// - Artifacts/phase35-fluent-window.png and phase35-fluent-sample-button.png exist
    /// </remarks>
    [Fact]
    public async Task Screenshot_SampleButton_IsSmallerThanWindow()
    {
        await using var app = await LaunchAsync();
        await app.GetByAutomationId("SampleButton").WaitForAsync();
        var window = await app.ScreenshotAsync();
        var clip = await app.GetByAutomationId("SampleButton").ScreenshotAsync();
        AssertPng(clip);
        Assert.True(
            clip.Width < window.Width || clip.Height < window.Height,
            $"Element clip should be smaller than the window screenshot. clip={clip.Width}x{clip.Height} window={window.Width}x{window.Height} bytes={clip.PngBytes.Length}/{window.PngBytes.Length}."
        );
        await SaveArtifactAsync(window, "phase35-fluent-window.png");
        await SaveArtifactAsync(clip, "phase35-fluent-sample-button.png");
    }

    /// <summary>
    /// Open Popup is composited with its PlacementTarget opener.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SamplePhase29bOpenPopup opens SamplePhase29bPopupButton
    ///
    /// Steps:
    /// - Launch sample
    /// - Screenshot OpenPopup closed, Invoke, screenshot OpenPopup and PopupButton
    /// - SaveAsync to Artifacts
    ///
    /// Expected:
    /// - Open opener clip is larger than the closed opener clip
    /// - PNG signature and width/height &gt; 0
    /// - Artifacts/phase35-fluent-popup-button.png exists (host + popup)
    /// </remarks>
    [Fact]
    public async Task Screenshot_OpenPopupButton_ReturnsPng()
    {
        await using var app = await LaunchAsync();
        await app.GetByAutomationId("SamplePhase29bOpenPopup").ScrollIntoViewAsync();
        var closed = await app.GetByAutomationId("SamplePhase29bOpenPopup").ScreenshotAsync();
        AssertPng(closed);
        await app.GetByAutomationId("SamplePhase29bOpenPopup").InvokeAsync();
        var opener = await app.GetByAutomationId("SamplePhase29bOpenPopup").ScreenshotAsync();
        AssertPng(opener);
        Assert.True(
            opener.Width > closed.Width || opener.Height > closed.Height,
            $"Open Popup should be composited with the opener. closed={closed.Width}x{closed.Height} open={opener.Width}x{opener.Height}."
        );
        await SaveArtifactAsync(opener, "phase35-fluent-popup-opener.png");
        var clip = await app.GetByAutomationId("SamplePhase29bPopupButton").ScreenshotAsync();
        AssertPng(clip);
        await SaveArtifactAsync(clip, "phase35-fluent-popup-button.png");
    }

    /// <summary>
    /// Open ToolTip is composited with its host, ancestor container, and target window.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SamplePhase29bTipSection contains SamplePhase29bTipHost with an openable ToolTip
    ///
    /// Steps:
    /// - Launch sample
    /// - Hover TipHost and ExpectToolTip
    /// - ScreenshotAsync on TipHost, ToolTip node, TipSection, and session
    /// - SaveAsync to Artifacts
    ///
    /// Expected:
    /// - PNGs have signature and width/height &gt; 0
    /// - Section clip is larger than host+tip and smaller than the window shot
    /// - Artifacts/phase35-fluent-tooltip.png, phase35-fluent-section-with-tooltip.png, phase35-fluent-window-with-tooltip.png exist
    /// </remarks>
    [Fact]
    public async Task Screenshot_OpenToolTip_ReturnsPng()
    {
        await using var app = await LaunchAsync();
        await app.GetByAutomationId("SamplePhase29bTipHost").ScrollIntoViewAsync();
        await app.GetByAutomationId("SamplePhase29bTipHost").HoverAsync();
        await app.GetByAutomationId("SamplePhase29bTipHost").ExpectToolTipAsync("Phase29bTip");
        await app.GetByControlType("ToolTip").ExpectNameAsync("Phase29bTip");
        var hostAndTip = await app.GetByAutomationId("SamplePhase29bTipHost").ScreenshotAsync();
        AssertPng(hostAndTip);
        await SaveArtifactAsync(hostAndTip, "phase35-fluent-tooltip.png");
        var tipAndHost = await app.GetByControlType("ToolTip").ScreenshotAsync();
        AssertPng(tipAndHost);
        await SaveArtifactAsync(tipAndHost, "phase35-fluent-tooltip-from-node.png");
        var sectionWithTip = await app.GetByAutomationId("SamplePhase29bTipSection")
            .ScreenshotAsync();
        AssertPng(sectionWithTip);
        Assert.True(
            sectionWithTip.Width > hostAndTip.Width || sectionWithTip.Height > hostAndTip.Height,
            $"Section screenshot should be larger than the host+tip clip. section={sectionWithTip.Width}x{sectionWithTip.Height} clip={hostAndTip.Width}x{hostAndTip.Height}."
        );
        await SaveArtifactAsync(sectionWithTip, "phase35-fluent-section-with-tooltip.png");
        var windowWithTip = await app.ScreenshotAsync();
        AssertPng(windowWithTip);
        Assert.True(
            windowWithTip.Width > sectionWithTip.Width
                || windowWithTip.Height > sectionWithTip.Height,
            $"Window screenshot should be larger than the section clip. window={windowWithTip.Width}x{windowWithTip.Height} section={sectionWithTip.Width}x{sectionWithTip.Height}."
        );
        await SaveArtifactAsync(windowWithTip, "phase35-fluent-window-with-tooltip.png");
    }

    private static Task<GraftSession> LaunchAsync() =>
        Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

    private static Task SaveArtifactAsync(Screenshot shot, string fileName) =>
        shot.SaveAsync(Path.Combine(AppContext.BaseDirectory, "Artifacts", fileName));

    private static void AssertPng(Screenshot shot)
    {
        Assert.Equal("png", shot.Format);
        Assert.True(shot.Width > 0);
        Assert.True(shot.Height > 0);
        Assert.True(shot.PngBytes.Length >= 8);
        Assert.True(
            shot.PngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature),
            "Expected PNG signature."
        );
    }
}
