using Graft.Core.Diagnostics;

namespace Graft.Core.Tests;

public sealed class OperationTimelineTests
{
    /// <summary>
    /// Always retention writes PNG frames, timeline.json, and index.html.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake PNG capture callback
    ///
    /// Steps:
    /// - CaptureAfterAsync twice with labels
    /// - FinalizeArtifacts
    ///
    /// Expected:
    /// - index.html / timeline.json / frames exist; labels appear in HTML
    /// </remarks>
    [Fact]
    public async Task Finalize_Always_WritesHtmlAndPngFrames()
    {
        var dir = NewTempDir();
        try
        {
            var png = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2, 3 };
            var timeline = new OperationTimeline(
                new TimelineOptions
                {
                    OutputDirectory = dir,
                    Retention = TimelineRetention.Always,
                    FrameDelayMilliseconds = 500,
                },
                _ => Task.FromResult(png)
            );

            await timeline.CaptureAfterAsync("invoke", "SampleButton");
            await timeline.CaptureAfterAsync("expectName", "Ready");

            var index = timeline.FinalizeArtifacts();
            Assert.NotNull(index);
            Assert.True(File.Exists(index));
            Assert.True(File.Exists(Path.Combine(dir, "timeline.json")));
            Assert.True(File.Exists(Path.Combine(dir, "frames", "0001.png")));
            Assert.True(File.Exists(Path.Combine(dir, "frames", "0002.png")));
            var html = await File.ReadAllTextAsync(index);
            Assert.Contains("invoke:SampleButton", html, StringComparison.Ordinal);
            Assert.Contains("expectName:Ready", html, StringComparison.Ordinal);
            Assert.Contains("0.5x", html, StringComparison.Ordinal);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    /// <summary>
    /// Supplied PNG bytes are stored as the frame instead of recapturing the window.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Window-capture callback returns a distinct PNG
    /// - Clip PNG bytes are provided to CaptureAfterAsync
    ///
    /// Steps:
    /// - CaptureAfterAsync with pngBytes
    /// - FinalizeArtifacts
    ///
    /// Expected:
    /// - Frame file equals the clip bytes
    /// - Window-capture callback is not invoked
    /// </remarks>
    [Fact]
    public async Task CaptureAfter_WithPngBytes_WritesThoseBytesNotWindowCapture()
    {
        var dir = NewTempDir();
        try
        {
            var captures = 0;
            var windowPng = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 9, 9, 9 };
            var clipPng = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2, 3 };
            var timeline = new OperationTimeline(
                new TimelineOptions
                {
                    OutputDirectory = dir,
                    Retention = TimelineRetention.Always,
                },
                _ =>
                {
                    captures++;
                    return Task.FromResult(windowPng);
                }
            );

            await timeline.CaptureAfterAsync(
                "screenshot",
                "12x8:7",
                CancellationToken.None,
                clipPng
            );

            var index = timeline.FinalizeArtifacts();
            Assert.NotNull(index);
            Assert.Equal(0, captures);
            var frame = Path.Combine(dir, "frames", "0001.png");
            Assert.True(File.Exists(frame));
            Assert.Equal(clipPng, await File.ReadAllBytesAsync(frame));
        }
        finally
        {
            TryDelete(dir);
        }
    }

    /// <summary>
    /// OnFailure discards artifacts when MarkFailed was never called.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Retention OnFailure; one captured frame
    ///
    /// Steps:
    /// - FinalizeArtifacts without MarkFailed
    ///
    /// Expected:
    /// - null path; output directory removed
    /// </remarks>
    [Fact]
    public async Task Finalize_OnFailure_DiscardsWhenClean()
    {
        var dir = NewTempDir();
        try
        {
            var png = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' };
            var timeline = new OperationTimeline(
                new TimelineOptions
                {
                    OutputDirectory = dir,
                    Retention = TimelineRetention.OnFailure,
                },
                _ => Task.FromResult(png)
            );

            await timeline.CaptureAfterAsync("invoke", "x");
            var index = timeline.FinalizeArtifacts();
            Assert.Null(index);
            Assert.False(Directory.Exists(dir));
        }
        finally
        {
            TryDelete(dir);
        }
    }

    /// <summary>
    /// OnFailure keeps artifacts after MarkFailed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Retention OnFailure; MarkFailed called
    ///
    /// Steps:
    /// - Capture + MarkFailed + Finalize
    ///
    /// Expected:
    /// - index.html kept
    /// </remarks>
    [Fact]
    public async Task Finalize_OnFailure_KeepsWhenMarkedFailed()
    {
        var dir = NewTempDir();
        try
        {
            var png = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' };
            var timeline = new OperationTimeline(
                new TimelineOptions
                {
                    OutputDirectory = dir,
                    Retention = TimelineRetention.OnFailure,
                },
                _ => Task.FromResult(png)
            );

            await timeline.CaptureAfterAsync("invoke", "x");
            timeline.MarkFailed();
            var index = timeline.FinalizeArtifacts();
            Assert.NotNull(index);
            Assert.True(File.Exists(index));
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static string NewTempDir() =>
        Path.Combine(Path.GetTempPath(), "graft-timeline-tests", Guid.NewGuid().ToString("N"));

    private static void TryDelete(string dir)
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
