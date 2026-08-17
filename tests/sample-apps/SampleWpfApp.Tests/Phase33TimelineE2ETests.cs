using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase33TimelineE2ETests
{
    /// <summary>
    /// Opt-in timeline records post-operation PNGs and an HTML viewer on dispose.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp Launch with Timeline Always and a unique output directory
    ///
    /// Steps:
    /// - ExpectNameAsync StatusText Ready
    /// - InvokeAsync SampleButton
    /// - Dispose session (auto finalize)
    ///
    /// Expected:
    /// - index.html and at least one frame PNG exist; HTML contains invoke label
    /// </remarks>
    [Fact]
    public async Task Timeline_Always_WritesViewerAfterOperations()
    {
        var outDir = Path.Combine(Path.GetTempPath(), "graft-timeline-e2e", Guid.NewGuid().ToString("N"));

        try
        {
            await using (
                var app = await Application.LaunchAsync(
                    new LaunchOptions
                    {
                        AppPath = SampleAppLocator.ResolveProjectPath(),
                        Configuration = "GraftTest",
                        Timeout = TimeSpan.FromSeconds(60),
                        Timeline = new TimelineOptions { OutputDirectory = outDir, Retention = TimelineRetention.Always },
                    }
                )
            )
            {
                await app.GetByAutomationId("StatusText").ExpectNameAsync("Ready");
                await app.GetByAutomationId("SampleButton").InvokeAsync();
                await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
            }

            var index = Path.Combine(outDir, "index.html");
            Assert.True(File.Exists(index));
            Assert.True(Directory.Exists(Path.Combine(outDir, "frames")));
            Assert.NotEmpty(Directory.GetFiles(Path.Combine(outDir, "frames"), "*.png"));
            var html = await File.ReadAllTextAsync(index);
            Assert.Contains("invoke:SampleButton", html, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }
        }
    }
}
