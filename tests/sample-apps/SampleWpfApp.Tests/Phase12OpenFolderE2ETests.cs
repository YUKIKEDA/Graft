using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase12OpenFolderE2ETests
{
    /// <summary>
    /// ArmOpenFolder then InvokeOpeningWindow (no new window) updates StatusText with the path.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample OpenFolderButton uses Microsoft.Win32.OpenFolderDialog
    ///
    /// Steps:
    /// - ArmOpenFolderAsync with a path
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false) on OpenFolderButton
    /// - ExpectNameAsync on StatusText
    ///
    /// Expected:
    /// - StatusText is "OpenFolder {path}"
    /// </remarks>
    [Fact]
    public async Task ArmOpenFolder_ThenInvoke_UpdatesStatusWithPath()
    {
        const string path = @"C:\graft-open-folder-ok";

        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmOpenFolderAsync(path);
        _ = await app.GetByAutomationId("OpenFolderButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync($"OpenFolder {path}");
    }

    /// <summary>
    /// ArmOpenFolderCancel then InvokeOpeningWindow updates StatusText to OpenFolderCancel.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample OpenFolderButton uses Microsoft.Win32.OpenFolderDialog
    ///
    /// Steps:
    /// - ArmOpenFolderCancelAsync
    /// - InvokeOpeningWindowAsync(waitForNewWindow: false)
    /// - ExpectNameAsync("OpenFolderCancel")
    ///
    /// Expected:
    /// - StatusText is OpenFolderCancel
    /// </remarks>
    [Fact]
    public async Task ArmOpenFolderCancel_ThenInvoke_UpdatesStatusCancel()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.ArmOpenFolderCancelAsync();
        _ = await app.GetByAutomationId("OpenFolderButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("StatusText").ExpectNameAsync("OpenFolderCancel");
    }
}
