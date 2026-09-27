using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Graft.Core;

/// <summary>
/// Ties launched application processes to the lifetime of the current (controller) process.
/// </summary>
/// <remarks>
/// On Windows every launched process is assigned to one process-wide Job Object created with
/// <c>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</c>. The job handle is intentionally never closed: when the
/// test runner or MCP server exits for any reason (including a forced kill or CI timeout), the OS
/// closes the handle and terminates every process still in the job, so no orphaned app survives.
/// Child processes created later (e.g. the app started by <c>dotnet run</c>) inherit the job.
/// Assignment is best-effort; on other platforms, or if the OS refuses, this is a no-op and
/// cleanup relies on <see cref="GraftSession.DisposeAsync"/> as before.
/// </remarks>
internal static class ChildProcessJob
{
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JobObjectLimitKillOnJobClose = 0x2000;

    private static readonly Lazy<IntPtr> Job = new(CreateJob, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the job handle, or <see cref="IntPtr.Zero"/> when unavailable (non-Windows or creation failed).
    /// </summary>
    internal static IntPtr Handle => OperatingSystem.IsWindows() ? Job.Value : IntPtr.Zero;

    /// <summary>
    /// Assigns <paramref name="process"/> to the kill-on-close job.
    /// </summary>
    /// <param name="process">A started process.</param>
    /// <returns><see langword="true"/> when the process is now in the job.</returns>
    public static bool TryAssign(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var job = Handle;
        if (job == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            if (AssignProcessToJobObject(job, process.Handle))
            {
                return true;
            }

            Trace.TraceWarning(
                $"Graft: AssignProcessToJobObject failed (Win32={Marshal.GetLastWin32Error()}); the app will not be killed if this process dies."
            );
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Process already exited.
        }

        return false;
    }

    private static IntPtr CreateJob()
    {
        if (!OperatingSystem.IsWindows())
        {
            return IntPtr.Zero;
        }

        var job = CreateJobObjectW(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var info = new JobObjectExtendedLimitInformationData
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation { LimitFlags = JobObjectLimitKillOnJobClose },
        };

        var length = Marshal.SizeOf<JobObjectExtendedLimitInformationData>();
        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(info, buffer, fDeleteOld: false);
            if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, buffer, (uint)length))
            {
                Trace.TraceWarning($"Graft: SetInformationJobObject failed (Win32={Marshal.GetLastWin32Error()}).");
                _ = CloseHandle(job);
                return IntPtr.Zero;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return job;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObjectW(IntPtr jobAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint infoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformationData
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
