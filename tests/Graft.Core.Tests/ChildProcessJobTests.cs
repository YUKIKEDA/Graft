using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Graft.Core.Tests;

/// <summary>
/// Kill-on-close Job Object assignment for launched processes (#89 / #98).
/// </summary>
public sealed class ChildProcessJobTests
{
    /// <summary>
    /// A started process is assigned to the controller's kill-on-close job on Windows (no-op elsewhere).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A long-running child process (ping on Windows, sleep elsewhere)
    ///
    /// Steps:
    /// - ChildProcessJob.TryAssign(process)
    /// - On Windows, query IsProcessInJob against ChildProcessJob.Handle
    ///
    /// Expected:
    /// - Windows: TryAssign true and the process is in the job
    /// - Other OS: TryAssign false (best-effort no-op)
    /// </remarks>
    [Fact]
    public void TryAssign_PutsProcessInKillOnCloseJob_OnWindows()
    {
        var psi = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("ping", "-n 30 127.0.0.1")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            }
            : new ProcessStartInfo("sleep", "30") { UseShellExecute = false };

        using var process = Process.Start(psi)!;
        try
        {
            var assigned = ChildProcessJob.TryAssign(process);

            if (!OperatingSystem.IsWindows())
            {
                Assert.False(assigned);
                return;
            }

            Assert.True(assigned);
            Assert.True(IsProcessInJob(process.Handle, ChildProcessJob.Handle, out var inJob));
            Assert.True(inJob);
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool result);
}
