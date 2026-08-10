using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 17 TabControl select acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase17TabControlE2ETests
{
    /// <summary>
    /// SelectAsync on SampleTabs selects Tab B and updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - SampleTabs has SampleTabA / SampleTabB
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleTabs").SelectAsync(1)
    /// - ExpectSelectedAsync(true) on SampleTabB
    /// - ExpectNameAsync("Tab B") on StatusText
    ///
    /// Expected:
    /// - Tab B selected; StatusText is Tab B
    /// </remarks>
    [Fact]
    public async Task Select_SampleTabs_SelectsTabB()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleTabs").SelectAsync(1);
        await app.GetByAutomationId("SampleTabB").ExpectSelectedAsync(true);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Tab B");
    }
}
