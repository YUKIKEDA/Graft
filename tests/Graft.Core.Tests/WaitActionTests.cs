namespace Graft.Core.Tests;

public sealed class WaitActionTests
{
    /// <summary>
    /// ExpectNameAsync fails with expect.failed when the name never matches.
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
    /// </remarks>
    [Fact]
    public async Task ExpectName_WrongValue_ThrowsExpectFailed()
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
        Assert.Equal(Graft.Protocol.GraftErrorCodes.ExpectFailed, ex.Code);
    }
}
