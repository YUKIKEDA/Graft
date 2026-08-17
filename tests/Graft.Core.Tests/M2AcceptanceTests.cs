namespace Graft.Core.Tests;

/// <summary>
/// M2 milestone acceptance: Core Launch path without SmokeClient.
/// </summary>
[Collection(SampleUiCollection.Name)]
[Trait("Category", "UI")]
public sealed class M2AcceptanceTests
{
    /// <summary>
    /// Launch SampleWpfApp, invoke SampleButton, expect StatusText becomes Clicked 1.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp.csproj is available under the repo
    /// - GraftTest configuration enables GRAFT_TEST / agent
    ///
    /// Steps:
    /// - Application.LaunchAsync(SampleWpfApp)
    /// - GetByAutomationId("SampleButton").InvokeAsync()
    /// - GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1")
    ///
    /// Expected:
    /// - StatusText name is "Clicked 1"
    /// - SmokeClient is not used
    /// </remarks>
    [Fact]
    public async Task Launch_InvokeSampleButton_ExpectStatusClicked1()
    {
        var appPath = SampleAppPaths.ResolveSampleWpfAppProject();
        await using var session = await Application.LaunchAsync(new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(60) });

        await session.GetByAutomationId("SampleButton").InvokeAsync();
        var status = await session.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
        Assert.Equal("Clicked 1", status.Name);
    }
}
