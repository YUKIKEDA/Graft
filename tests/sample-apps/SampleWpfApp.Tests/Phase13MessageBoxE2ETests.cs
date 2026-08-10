using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase13MessageBoxE2ETests
{
    /// <summary>
    /// ArmMessageBox Yes then InvokeOpeningWindow updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample MessageBoxButton uses System.Windows.MessageBox.Show YesNo
    ///
    /// Steps:
    /// - ArmMessageBoxAsync Yes
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false)
    /// - ExpectNameAsync
    ///
    /// Expected:
    /// - StatusText is "MessageBox Yes"
    /// </remarks>
    [Fact]
    public async Task ArmYes_ThenInvoke_UpdatesStatusYes()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmMessageBoxAsync("Yes");
        _ = await app.GetByAutomationId("MessageBoxButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MessageBox Yes");
    }

    /// <summary>
    /// ArmMessageBox No then InvokeOpeningWindow updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample MessageBoxButton uses System.Windows.MessageBox.Show YesNo
    ///
    /// Steps:
    /// - ArmMessageBoxAsync No
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false)
    /// - ExpectNameAsync
    ///
    /// Expected:
    /// - StatusText is "MessageBox No"
    /// </remarks>
    [Fact]
    public async Task ArmNo_ThenInvoke_UpdatesStatusNo()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmMessageBoxAsync("No");
        _ = await app.GetByAutomationId("MessageBoxButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MessageBox No");
    }
}
