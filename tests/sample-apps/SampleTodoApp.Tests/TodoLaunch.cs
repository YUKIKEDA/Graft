using Graft.Core;

namespace SampleTodoApp.Tests;

internal static class TodoLaunch
{
    /// <summary>
    /// Resolves the timeline output directory paired with a data directory
    /// (<c>%TEMP%\graft-sample-todo-timeline\{dataDir leaf}\</c>).
    /// </summary>
    public static string ResolveTimelineDirectory(string dataDir)
    {
        var leaf = Path.GetFileName(
            dataDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        if (string.IsNullOrWhiteSpace(leaf))
        {
            leaf = Guid.NewGuid().ToString("N");
        }

        return Path.Combine(Path.GetTempPath(), "graft-sample-todo-timeline", leaf);
    }

    public static async Task<GraftSession> LaunchAsync(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        var timelineDir = ResolveTimelineDirectory(dataDir);
        Directory.CreateDirectory(timelineDir);

        var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = TodoAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(90),
                Timeline = new TimelineOptions
                {
                    OutputDirectory = timelineDir,
                    Retention = TimelineRetention.Always,
                },
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
