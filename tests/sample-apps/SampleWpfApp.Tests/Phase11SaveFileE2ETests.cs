using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase11SaveFileE2ETests
{
    /// <summary>
    /// ArmSaveFile then InvokeOpeningWindow (no new window) updates StatusText with the path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample SaveFileButton uses Microsoft.Win32.SaveFileDialog
    ///
    /// Steps:
    /// - ArmSaveFileAsync with a path
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false) on SaveFileButton
    /// - ExpectNameAsync on StatusText
    ///
    /// Expected:
    /// - StatusText is "SaveFile {path}"
    /// </remarks>
    [Fact]
    public async Task ArmSaveFile_ThenInvoke_UpdatesStatusWithPath()
    {
        const string path = @"C:\graft-save-file-ok.txt";

        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmSaveFileAsync(path);
        _ = await app.GetByAutomationId("SaveFileButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync($"SaveFile {path}");
    }

    /// <summary>
    /// ArmSaveFileCancel then InvokeOpeningWindow updates StatusText to SaveFileCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample SaveFileButton uses Microsoft.Win32.SaveFileDialog
    ///
    /// Steps:
    /// - ArmSaveFileCancelAsync
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false)
    /// - ExpectNameAsync("SaveFileCancel")
    ///
    /// Expected:
    /// - StatusText is SaveFileCancel
    /// </remarks>
    [Fact]
    public async Task ArmSaveFileCancel_ThenInvoke_UpdatesStatusCancel()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmSaveFileCancelAsync();
        _ = await app.GetByAutomationId("SaveFileButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("SaveFileCancel");
    }
}
