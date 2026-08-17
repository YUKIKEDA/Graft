using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase10OpenFileE2ETests
{
    /// <summary>
    /// ArmOpenFile then InvokeOpeningWindow (no new window) updates StatusText with the path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample OpenFileButton uses Microsoft.Win32.OpenFileDialog
    ///
    /// Steps:
    /// - ArmOpenFileAsync with a path
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false) on OpenFileButton
    /// - ExpectNameAsync on StatusText
    ///
    /// Expected:
    /// - StatusText is "OpenFile {path}"
    /// </remarks>
    [Fact]
    public async Task ArmOpenFile_ThenInvoke_UpdatesStatusWithPath()
    {
        const string path = @"C:\graft-open-file-ok.txt";

        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmOpenFileAsync(path);
        _ = await app.GetByAutomationId("OpenFileButton").InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync($"OpenFile {path}");
    }

    /// <summary>
    /// ArmOpenFileCancel then InvokeOpeningWindow updates StatusText to OpenFileCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample OpenFileButton uses Microsoft.Win32.OpenFileDialog
    ///
    /// Steps:
    /// - ArmOpenFileCancelAsync
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false)
    /// - ExpectNameAsync("OpenFileCancel")
    ///
    /// Expected:
    /// - StatusText is OpenFileCancel
    /// </remarks>
    [Fact]
    public async Task ArmOpenFileCancel_ThenInvoke_UpdatesStatusCancel()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmOpenFileCancelAsync();
        _ = await app.GetByAutomationId("OpenFileButton").InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("OpenFileCancel");
    }
}
