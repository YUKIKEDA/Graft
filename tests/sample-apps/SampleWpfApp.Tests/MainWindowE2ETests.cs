using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Example consumer E2E tests for <c>SampleWpfApp</c>.
/// </summary>
/// <remarks>
/// Role split (this is the usage pattern for product apps):
/// <list type="bullet">
/// <item>
/// <description>
/// App under test (<c>SampleWpfApp</c>): references <c>Graft.Instrumentation.Wpf</c>,
/// calls <c>WpfGraft.Use()</c> / <c>Agent.Start()</c> under <c>GRAFT_TEST</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Test project (this assembly): references <c>Graft.Core</c> only, launches the app,
/// then drives UI with <c>GetByAutomationId</c> / <c>InvokeAsync</c> / <c>ExpectNameAsync</c>.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class MainWindowE2ETests
{
    /// <summary>
    /// Clicking SampleButton updates StatusText to Clicked 1.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Application.LaunchAsync(sample csproj) — sets GRAFT_ENABLE / PIPE / TOKEN
    /// - GetByAutomationId("SampleButton").InvokeAsync()
    /// - GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1")
    ///
    /// Expected:
    /// - Expectation passes; disposing the session stops the app process
    /// </remarks>
    [Fact]
    public async Task ClickSampleButton_UpdatesStatusText()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleButton").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
    }
}
