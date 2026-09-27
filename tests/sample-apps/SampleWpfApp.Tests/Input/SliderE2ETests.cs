using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 18 Slider setValue acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class SliderE2ETests
{
    /// <summary>
    /// SetValueAsync on SampleSlider updates StatusText via ValueChanged.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - SampleSlider Minimum=0 Maximum=100
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleSlider").SetValueAsync("75")
    /// - ExpectNameAsync("Slider 75") on StatusText
    ///
    /// Expected:
    /// - StatusText is Slider 75
    /// </remarks>
    [Fact]
    public async Task SetValue_SampleSlider_UpdatesStatusText()
    {
        await using var app = await SampleWpfLaunch.LaunchAsync();

        await app.GetByAutomationId("SampleSlider").SetValueAsync("75");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Slider 75");
    }
}
