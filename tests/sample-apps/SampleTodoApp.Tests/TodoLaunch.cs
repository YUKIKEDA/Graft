using Graft.Core;

namespace SampleTodoApp.Tests;

internal static class TodoLaunch
{
    public static async Task<GraftSession> LaunchAsync(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = TodoAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(90),
            }
        );

        try
        {
            await app.GetByAutomationId("StatusText").WaitForAsync();
            await OpenSettingsAndSetDataDirectoryAsync(app, dataDir);
            return app;
        }
        catch
        {
            await app.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public static async Task OpenSettingsAndSetDataDirectoryAsync(GraftSession app, string dataDir)
    {
        await app.GetByAutomationId("SettingsButton").InvokeAsync();
        await app.GetByAutomationId("SettingsView").WaitForAsync();
        await app.ArmOpenFolderAsync(dataDir);
        _ = await app.GetByAutomationId("SettingsBrowseDataDirectoryButton")
            .InvokeOpeningWindowAsync(waitForNewWindow: false);
        await app.GetByAutomationId("SettingsCloseButton").InvokeAsync();
        await app.GetByAutomationId("SettingsView").ExpectGoneAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("DataDirectoryChanged");
    }
}
