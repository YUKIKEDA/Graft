using Graft.Core.Diagnostics;
using Graft.Core.Selectors;
using Graft.Protocol;

namespace Graft.Core.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class SelfHealTests
{
    /// <summary>
    /// Stale AutomationId with correct Name/ControlType/Near auto-heals and invokes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp with Window AutomationId=Main and SampleButton Automation Name=SampleClickMe
    ///
    /// Steps:
    /// - GetBy selector with wrong AutomationId + Name + ControlType + Near
    /// - InvokeAsync
    /// - Expect StatusText Clicked 1
    ///
    /// Expected:
    /// - Invoke succeeds via one-shot self-heal; status updates
    /// </remarks>
    [Fact]
    public async Task Invoke_StaleAutomationId_AutoHealsAndClicks()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(
            new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) }
        );

        await session
            .GetBy(
                new Selector
                {
                    AutomationId = "OldSampleButton",
                    Name = "SampleClickMe",
                    ControlType = "Button",
                    NearAutomationId = "Main",
                }
            )
            .InvokeAsync();

        await session.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
    }

    /// <summary>
    /// Missing AutomationId-only wait failure attaches healingCandidates.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp launched
    ///
    /// Steps:
    /// - Short ActionTimeout; InvokeAsync on missing automation id
    ///
    /// Expected:
    /// - GraftException with FailureReport.HealingCandidates non-empty
    /// </remarks>
    [Fact]
    public async Task Invoke_MissingAutomationId_FailureReportIncludesHealingCandidates()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(
            new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) }
        );
        session.WaitOptions = new WaitOptions
        {
            ActionTimeout = TimeSpan.FromMilliseconds(600),
            PollInterval = TimeSpan.FromMilliseconds(50),
        };

        var ex = await Assert.ThrowsAsync<GraftException>(() =>
            session.GetByAutomationId("DoesNotExist").InvokeAsync()
        );
        Assert.Equal(GraftErrorCodes.ActionTimeout, ex.Code);
        Assert.NotNull(ex.Report);
        Assert.NotNull(ex.Report.HealingCandidates);
        Assert.NotEmpty(ex.Report.HealingCandidates);
        Assert.Equal("DoesNotExist", ex.Report.Selector.AutomationId);
    }
}
