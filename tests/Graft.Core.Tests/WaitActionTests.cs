using Graft.Core.Diagnostics;
using Graft.Protocol;

namespace Graft.Core.Tests;

[Collection(SampleUiCollection.Name)]
[Trait("Category", "UI")]
public sealed class WaitActionTests
{
    /// <summary>
    /// ExpectNameAsync fails with expect.failed and attachments on FailureReport.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp launched; StatusText initial text is "Ready"
    ///
    /// Steps:
    /// - ExpectNameAsync("Ready") to ensure the element is present (records operation)
    /// - Set a short ExpectTimeout
    /// - ExpectNameAsync("never-matches")
    ///
    /// Expected:
    /// - GraftException with expect.failed
    /// - Report minimum fields plus tree and/or screenshotPath, and recentOperations
    /// </remarks>
    [Fact]
    public async Task ExpectName_WrongValue_ThrowsExpectFailedWithFailureReport()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) });
        session.WaitOptions = new WaitOptions { ExpectTimeout = TimeSpan.FromMilliseconds(800), PollInterval = TimeSpan.FromMilliseconds(50) };

        _ = await session.GetByAutomationId("StatusText").ExpectNameAsync("Ready");

        session.WaitOptions = new WaitOptions { ExpectTimeout = TimeSpan.FromMilliseconds(500), PollInterval = TimeSpan.FromMilliseconds(50) };

        var ex = await Assert.ThrowsAsync<GraftException>(() => session.GetByAutomationId("StatusText").ExpectNameAsync("never-matches"));
        Assert.Equal(GraftErrorCodes.ExpectFailed, ex.Code);
        Assert.NotNull(ex.Report);
        Assert.Equal(FailureSteps.ExpectName, ex.Report.Step);
        Assert.Equal("never-matches", ex.Report.Expected);
        Assert.Equal("Ready", ex.Report.Actual);
        Assert.True(ex.Report.TimedOut);
        Assert.Equal("StatusText", ex.Report.Selector.AutomationId);

        Assert.NotNull(ex.Report.Tree);
        Assert.False(string.IsNullOrWhiteSpace(ex.Report.Tree.ControlType));
        Assert.NotNull(ex.Report.RecentOperations);
        Assert.Contains(ex.Report.RecentOperations, op => op.Action == FailureSteps.ExpectName && op.Detail == "Ready");
        Assert.False(string.IsNullOrWhiteSpace(ex.Report.ScreenshotPath));
        Assert.True(File.Exists(ex.Report.ScreenshotPath));
        try
        {
            var png = await File.ReadAllBytesAsync(ex.Report.ScreenshotPath);
            Assert.True(png.Length >= 8);
            Assert.Equal(0x89, png[0]);
            Assert.Equal((byte)'P', png[1]);
            Assert.Equal((byte)'N', png[2]);
            Assert.Equal((byte)'G', png[3]);
        }
        finally
        {
            File.Delete(ex.Report.ScreenshotPath);
        }
    }
}
