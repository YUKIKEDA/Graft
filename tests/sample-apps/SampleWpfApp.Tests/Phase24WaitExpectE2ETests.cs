using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase24WaitExpectE2ETests
{
    /// <summary>
    /// Progress window fills, Close enables, then next-screen panel appears on Main.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp OpenProgressWindowButton opens ProgressWindow with SampleProgress
    /// - Closing ProgressWindow reveals NextScreenPanel on Main
    ///
    /// Steps:
    /// - ExpectGoneAsync on NextScreenLabel (collapsed)
    /// - Invoke OpenProgressWindowButton
    /// - WaitForWindowAsync ProgressWindow
    /// - WaitForAsync SampleProgress
    /// - ExpectValueAsync 100, ExpectEnabledAsync Close, ExpectNameContains/Matches ProgressStatus
    /// - Invoke CloseProgressButton → WaitForWindowClosedAsync → WaitForWindow Main
    /// - WaitForAsync NextScreenLabel, ExpectVisibleAsync NextScreenPanel
    /// - ExpectGoneAsync SampleProgress (not on Main tree)
    ///
    /// Expected:
    /// - Wait/Expect/value/window-closed path completes; next screen is visible
    /// </remarks>
    [Fact]
    public async Task ProgressWindow_WaitExpectValue_ThenNextScreenPanel()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("NextScreenLabel").ExpectGoneAsync();

        await app.GetByAutomationId("OpenProgressWindowButton").InvokeAsync();
        await app.WaitForWindowAsync(automationId: "ProgressWindow");

        await app.GetByAutomationId("SampleProgress").WaitForAsync();
        await app.GetByAutomationId("SampleProgress").ExpectValueAsync("100");
        await app.GetByAutomationId("CloseProgressButton").ExpectEnabledAsync(true);
        await app.GetByAutomationId("ProgressStatus").ExpectNameContainsAsync("Done");
        await app.GetByAutomationId("ProgressStatus").ExpectNameMatchesAsync("^ProgressDone$");

        await app.GetByAutomationId("CloseProgressButton").InvokeAsync();
        await app.WaitForWindowClosedAsync(automationId: "ProgressWindow");
        await app.WaitForWindowAsync(automationId: "Main");

        await app.GetByAutomationId("NextScreenLabel").WaitForAsync();
        await app.GetByAutomationId("NextScreenPanel").ExpectVisibleAsync(true);
        await app.GetByAutomationId("NextScreenLabel").ExpectNameAsync("NextScreenReady");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("ProgressClosed");
        await app.GetByAutomationId("SampleProgress").ExpectGoneAsync();
    }
}
