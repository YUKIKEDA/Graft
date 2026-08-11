using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase32FrameE2ETests
{
    /// <summary>
    /// Frame page navigation is observable via existing WaitFor / Expect APIs.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp hosts SampleFrame and loads FrameHomePage on Main Loaded
    /// - NavigateFrameAlpha / Beta / Home buttons swap Frame content pages
    ///
    /// Steps:
    /// - WaitForAsync FrameHomeLabel and ExpectNameAsync FrameHomeReady
    /// - ExpectGoneAsync FrameAlphaLabel / FrameBetaLabel
    /// - Invoke NavigateFrameAlphaButton → WaitForAsync FrameAlphaLabel
    /// - ExpectGoneAsync FrameHomeLabel; ExpectNameAsync FrameAlphaReady
    /// - Invoke NavigateFrameBetaButton → WaitForAsync FrameBetaLabel
    /// - ExpectGoneAsync FrameAlphaLabel; ExpectNameAsync FrameBetaReady
    /// - Invoke NavigateFrameHomeButton → WaitForAsync FrameHomeLabel
    ///
    /// Expected:
    /// - Current page labels are findable; previous page labels are gone
    /// </remarks>
    [Fact]
    public async Task Frame_NavigatePages_WaitForAndExpectLabels()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("FrameHomeLabel").WaitForAsync();
        await app.GetByAutomationId("FrameHomeLabel").ExpectNameAsync("FrameHomeReady");
        await app.GetByAutomationId("FrameAlphaLabel").ExpectGoneAsync();
        await app.GetByAutomationId("FrameBetaLabel").ExpectGoneAsync();
        await app.GetByAutomationId("SampleFrame").ExpectVisibleAsync(true);

        await app.GetByAutomationId("NavigateFrameAlphaButton").InvokeAsync();
        await app.GetByAutomationId("FrameAlphaLabel").WaitForAsync();
        await app.GetByAutomationId("FrameAlphaLabel").ExpectNameAsync("FrameAlphaReady");
        await app.GetByAutomationId("FrameHomeLabel").ExpectGoneAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("FrameAlpha");

        await app.GetByAutomationId("NavigateFrameBetaButton").InvokeAsync();
        await app.GetByAutomationId("FrameBetaLabel").WaitForAsync();
        await app.GetByAutomationId("FrameBetaLabel").ExpectNameAsync("FrameBetaReady");
        await app.GetByAutomationId("FrameAlphaLabel").ExpectGoneAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("FrameBeta");

        await app.GetByAutomationId("NavigateFrameHomeButton").InvokeAsync();
        await app.GetByAutomationId("FrameHomeLabel").WaitForAsync();
        await app.GetByAutomationId("FrameHomeLabel").ExpectNameAsync("FrameHomeReady");
        await app.GetByAutomationId("FrameBetaLabel").ExpectGoneAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("FrameHome");
    }
}
