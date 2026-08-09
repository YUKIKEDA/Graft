namespace Graft.Core.Tests;

public sealed class WaitActionTests
{
    /// <summary>
    /// Launch + GetBy invoke + ExpectName covers the M2 Core action path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp.csproj is available
    ///
    /// Steps:
    /// - Launch SampleWpfApp
    /// - GetByAutomationId(SampleButton).InvokeAsync
    /// - GetByAutomationId(StatusText).ExpectNameAsync("Clicked 1")
    ///
    /// Expected:
    /// - ExpectName returns a node whose name is Clicked 1
    /// </remarks>
    [Fact]
    public async Task Launch_InvokeSampleButton_ExpectStatusClicked1()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(
            new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) }
        );

        await session.GetByAutomationId("SampleButton").InvokeAsync();
        var status = await session.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
        Assert.Equal("Clicked 1", status.Name);
    }

    /// <summary>
    /// ExpectNameAsync times out with action.timeout when the name never matches.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp launched; StatusText starts as non-matching text
    ///
    /// Steps:
    /// - Set a short ExpectTimeout
    /// - ExpectNameAsync("never-matches")
    ///
    /// Expected:
    /// - GraftException with expect.failed (element present, wrong name) or action.timeout
    /// </remarks>
    [Fact]
    public async Task ExpectName_WrongValue_ThrowsExpectFailedOrTimeout()
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

        // Ensure StatusText is present (sample initial text is "Ready").
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
