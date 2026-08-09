using Graft.Core.Diagnostics;
using Graft.Protocol;

namespace Graft.Core.Tests;

public sealed class WaitActionTests
{
    /// <summary>
    /// ExpectNameAsync fails with expect.failed and a populated FailureReport.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp launched; StatusText initial text is "Ready"
    ///
    /// Steps:
    /// - ExpectNameAsync("Ready") to ensure the element is present
    /// - Set a short ExpectTimeout
    /// - ExpectNameAsync("never-matches")
    ///
    /// Expected:
    /// - GraftException with expect.failed
    /// - Report: step expectName, expected never-matches, actual Ready, timedOut true, selector StatusText
    /// </remarks>
    [Fact]
    public async Task ExpectName_WrongValue_ThrowsExpectFailedWithFailureReport()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(
            new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) }
        );
        session.WaitOptions = new WaitOptions
        {
            ExpectTimeout = TimeSpan.FromMilliseconds(800),
            PollInterval = TimeSpan.FromMilliseconds(50),
        };

        _ = await session.GetByAutomationId("StatusText").ExpectNameAsync("Ready");

        session.WaitOptions = new WaitOptions
        {
            ExpectTimeout = TimeSpan.FromMilliseconds(500),
            PollInterval = TimeSpan.FromMilliseconds(50),
        };

        var ex = await Assert.ThrowsAsync<GraftException>(() =>
            session.GetByAutomationId("StatusText").ExpectNameAsync("never-matches")
        );
        Assert.Equal(GraftErrorCodes.ExpectFailed, ex.Code);
        Assert.NotNull(ex.Report);
        Assert.Equal(FailureSteps.ExpectName, ex.Report.Step);
        Assert.Equal("never-matches", ex.Report.Expected);
        Assert.Equal("Ready", ex.Report.Actual);
        Assert.True(ex.Report.TimedOut);
        Assert.Equal("StatusText", ex.Report.Selector.AutomationId);
    }
}
