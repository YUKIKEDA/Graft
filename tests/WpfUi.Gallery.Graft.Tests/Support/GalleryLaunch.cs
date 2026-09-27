using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Shared Launch helpers for Gallery E2E (Timeline Always under %TEMP%).
/// </summary>
internal static class GalleryLaunch
{
    /// <summary>
    /// Resolves a unique timeline output directory under
    /// <c>%TEMP%\graft-wpfui-gallery-timeline\{leaf}\</c>.
    /// </summary>
    /// <param name="leaf">Directory leaf (category or run id).</param>
    /// <returns>Absolute timeline directory path.</returns>
    public static string ResolveTimelineDirectory(string leaf)
    {
        if (string.IsNullOrWhiteSpace(leaf))
        {
            leaf = Guid.NewGuid().ToString("N");
        }

        return Path.Combine(Path.GetTempPath(), "graft-wpfui-gallery-timeline", leaf);
    }

    /// <summary>
    /// Launches Gallery with <c>Configuration=GraftTest</c> and Timeline Always.
    /// </summary>
    /// <param name="timelineLeaf">Timeline directory leaf.</param>
    /// <returns>Open session and timeline directory.</returns>
    public static async Task<(GraftSession Session, string TimelineDir)> LaunchAsync(string timelineLeaf)
    {
        var timelineDir = ResolveTimelineDirectory(timelineLeaf);
        Directory.CreateDirectory(timelineDir);

        var session = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = GalleryAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(180),
                Timeline = new TimelineOptions { OutputDirectory = timelineDir, Retention = TimelineRetention.Always },
            }
        );

        return (session, timelineDir);
    }

    /// <summary>
    /// Asserts Timeline Always wrote <c>index.html</c> and at least one PNG frame.
    /// </summary>
    /// <param name="timelineDir">Timeline output directory.</param>
    public static void AssertTimelineWritten(string timelineDir)
    {
        var index = Path.Combine(timelineDir, "index.html");
        Assert.True(File.Exists(index), $"Timeline index missing: {index}");
        var framesDir = Path.Combine(timelineDir, "frames");
        Assert.True(Directory.Exists(framesDir), $"Timeline frames dir missing: {framesDir}");
        Assert.NotEmpty(Directory.GetFiles(framesDir, "*.png"));
    }

    /// <summary>
    /// Best-effort recursive delete.
    /// </summary>
    /// <param name="dir">Directory to delete.</param>
    public static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
