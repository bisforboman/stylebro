using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StyleBro.Migrate;

/// <summary>
/// Ends the 'dotnet format' processes this process starts when it ends, also when it's killed (trial 25: a killed 'format'
/// left its 'dotnet format' runs writing files): on Windows a job object that kills its processes when its last handle
/// closes, which the system does when this process ends. Only 'dotnet format' goes in (and what it starts: the forwarding
/// 'dotnet' process, Roslyn's build host), never a restore or build, whose reusable MSBuild nodes may serve other builds.
/// ponytail: Windows only; elsewhere a killed parent still leaves them running (a process group would do it there).
/// </summary>
internal static class ChildProcesses
{
    private const int ExtendedLimitInformation = 9;
    private const uint KillOnJobClose = 0x2000;
    private static readonly Lazy<IntPtr> Job = new(Create);

    /// <summary>Puts a started process in the job (nothing elsewhere than on Windows, or when the job couldn't be made).</summary>
    public static void Track(Process process)
    {
        if (!OperatingSystem.IsWindows() || Job.Value == IntPtr.Zero)
        {
            return;
        }

        try
        {
            NativeMethods.AssignProcessToJobObject(Job.Value, process.Handle);
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }

    private static IntPtr Create()
    {
        if (!OperatingSystem.IsWindows())
        {
            return IntPtr.Zero;
        }

        var job = NativeMethods.CreateJobObject(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
        {
            return job;
        }

        var info = new NativeMethods.ExtendedLimit { Basic = new NativeMethods.BasicLimit { LimitFlags = KillOnJobClose } };
        var size = Marshal.SizeOf<NativeMethods.ExtendedLimit>();
        var pointer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(info, pointer, false);
            return NativeMethods.SetInformationJobObject(job, ExtendedLimitInformation, pointer, (uint)size) ? job : IntPtr.Zero;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateJobObject(IntPtr attributes, string? name);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint length);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [StructLayout(LayoutKind.Sequential)]
        public struct BasicLimit
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
        public struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ExtendedLimit
        {
            public BasicLimit Basic;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
