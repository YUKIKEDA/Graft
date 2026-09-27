using System.Diagnostics;
using Graft.Protocol;

namespace Graft.Core.Tests;

/// <summary>
/// Launch diagnostics that do not need an instrumented app (#97).
/// </summary>
public sealed class LaunchDiagnosticsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "graft-launch-" + Guid.NewGuid().ToString("N"));

    public LaunchDiagnosticsTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort temp cleanup.
        }
    }

    /// <summary>
    /// A target process that exits before the handshake fails fast with its exit code and output.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A script that writes "graft-boom" to stderr and exits with code 3 (no Graft agent)
    ///
    /// Steps:
    /// - Application.LaunchAsync with a 30 s timeout
    ///
    /// Expected:
    /// - GraftException action.failed well before the timeout
    /// - Message contains the exit code, the GraftTest hint, and the captured stderr line
    /// </remarks>
    [Fact]
    public async Task Launch_ProcessExitsBeforeHandshake_FailsFastWithDiagnostics()
    {
        var appPath = CreateExitingScript(exitCode: 3, stderrLine: "graft-boom");

        var stopwatch = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<GraftException>(() =>
            Application.LaunchAsync(new LaunchOptions { AppPath = appPath, Timeout = TimeSpan.FromSeconds(30) })
        );
        stopwatch.Stop();

        Assert.Equal(GraftErrorCodes.ActionFailed, ex.Code);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15), $"Expected fail-fast, took {stopwatch.Elapsed}.");
        Assert.Contains("exit code 3", ex.Message, StringComparison.Ordinal);
        Assert.Contains("GraftTest", ex.Message, StringComparison.Ordinal);
        Assert.Contains("graft-boom", ex.Message, StringComparison.Ordinal);
    }

    private string CreateExitingScript(int exitCode, string stderrLine)
    {
        if (OperatingSystem.IsWindows())
        {
            var cmd = Path.Combine(_directory, "exit.cmd");
            File.WriteAllText(cmd, $"@echo off\r\necho {stderrLine} 1>&2\r\nexit /b {exitCode}\r\n");
            return cmd;
        }

        var sh = Path.Combine(_directory, "exit.sh");
        File.WriteAllText(sh, $"#!/bin/sh\necho {stderrLine} >&2\nexit {exitCode}\n");
        File.SetUnixFileMode(sh, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return sh;
    }
}
