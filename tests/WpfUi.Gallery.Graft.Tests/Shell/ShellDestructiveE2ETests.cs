using System.Diagnostics;
using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Destructive TitleBar Close (own process; must not share Shell session).
/// </summary>
[Collection(GalleryShellDestructiveCollection.Name)]
public sealed class ShellDestructiveE2ETests
{
    /// <summary>
    /// TitleBar Close exits the Gallery process.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Dedicated destructive collection (no shared Shell fixture)
    /// - TitleBarCloseButton AutomationId
    ///
    /// Steps:
    /// - Launch Gallery Timeline Always
    /// - Invoke TitleBarCloseButton
    /// - Wait until process has exited
    ///
    /// Expected:
    /// - Process exits; Timeline asserts when index.html exists
    /// </remarks>
    [Fact]
    public async Task TitleBar_Close_ExitsProcess()
    {
        if (!GalleryAppLocator.IsAvailable)
        {
            throw new InvalidOperationException("Gallery was not launched. Ensure tests/wpfui is cloned and Graft-patched.");
        }

        var leaf = $"shell-close-{Guid.NewGuid():N}";
        var (session, timelineDir) = await GalleryLaunch.LaunchAsync(leaf);
        var pid = session.ProcessId;
        Assert.True(pid > 0);

        try
        {
            GalleryNav.ApplyDefaultWaits(session);
            await session.WaitForWindowAsync(automationId: "MainWindow");
            try
            {
                await session.GetByAutomationId("TitleBarCloseButton").InvokeAsync();
            }
            catch (GraftException)
            {
                // Closing the process often drops the pipe before the Invoke response returns.
            }

            var exited = false;
            for (var i = 0; i < 40; i++)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    if (p.HasExited)
                    {
                        exited = true;
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    exited = true;
                    break;
                }

                await Task.Delay(250);
            }

            Assert.True(exited, $"Gallery process {pid} did not exit after TitleBarCloseButton.");
        }
        finally
        {
            await session.DisposeAsync();
            if (Directory.Exists(timelineDir) && File.Exists(Path.Combine(timelineDir, "index.html")))
            {
                GalleryLaunch.AssertTimelineWritten(timelineDir);
            }
        }
    }
}
