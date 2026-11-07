using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace TimesheetAssistant.UiTests;

// Private job membership identifies the CDP listener and bounds cleanup to our tree.
internal sealed class OwnedProcessJob : IDisposable
{
    private readonly SafeFileHandle handle = CreateJobObject(IntPtr.Zero, null);
    public OwnedProcessJob()
    {
        if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        var limits = new ExtendedLimits();
        limits.Basic.LimitFlags = 0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        if (!SetInformationJobObject(handle, 9, ref limits, Marshal.SizeOf<ExtendedLimits>()))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public Process StartApplication(ProcessStartInfo start, TimeSpan cleanupTimeout)
    {
        // Only this EXE is launched: no shell or arguments. Keep the child's environment private.
        var environment = string.Join('\0', start.Environment.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key}={pair.Value}")) + "\0\0";
        var environmentBlock = Marshal.StringToHGlobalUni(environment);
        try
        {
            var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
            const uint createSuspended = 0x00000004;
            const uint createUnicodeEnvironment = 0x00000400;
            if (!CreateProcess(start.FileName, new StringBuilder($"\"{start.FileName}\""), IntPtr.Zero, IntPtr.Zero,
                false, createSuspended | createUnicodeEnvironment, environmentBlock, start.WorkingDirectory,
                ref startup, out var info))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create suspended desktop process.");

            using var processHandle = new SafeFileHandle(info.Process, ownsHandle: true);
            using var threadHandle = new SafeFileHandle(info.Thread, ownsHandle: true);
            Process? process = null;
            try
            {
                // The primary thread cannot create WebView2 children before job membership is established.
                if (!AssignProcessToJobObject(handle, processHandle.DangerousGetHandle()))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not isolate suspended desktop process in a Windows job.");
                process = Process.GetProcessById(checked((int)info.ProcessId));
                if (ResumeThread(threadHandle) == uint.MaxValue)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resume isolated desktop process.");
                return process;
            }
            catch
            {
                // Also handles assignment failure, when job cleanup cannot reach this suspended process.
                try
                {
                    if (!TerminateProcess(processHandle, 1))
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not terminate failed desktop launch.");
                    if (WaitForSingleObject(processHandle, checked((uint)cleanupTimeout.TotalMilliseconds)) != 0)
                        throw new TimeoutException("Failed desktop launch did not exit within the cleanup budget.");
                    throw;
                }
                finally { process?.Dispose(); }
            }
        }
        finally { Marshal.FreeHGlobal(environmentBlock); }
    }
    public bool Contains(uint pid)
    {
        using var process = Process.GetProcessById(checked((int)pid));
        if (!IsProcessInJob(process.Handle, handle, out var result)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return result;
    }
    public bool HasProcesses
    {
        get
        {
            if (!QueryInformationJobObject(handle, 1, out var info, Marshal.SizeOf<Accounting>(), IntPtr.Zero)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return info.ActiveProcesses != 0;
        }
    }
    public void Terminate()
    {
        if (!TerminateJobObject(handle, 1)) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public static uint? ListenerOwner(int port)
    {
        var size = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 3, 0);
        if (result != 122 && result != 0) throw new Win32Exception((int)result);
        var table = Marshal.AllocHGlobal(size);
        try
        {
            result = GetExtendedTcpTable(table, ref size, false, 2, 3, 0);
            if (result != 0) throw new Win32Exception((int)result);
            for (var index = 0; index < Marshal.ReadInt32(table); index++)
            {
                var row = Marshal.PtrToStructure<TcpRow>(IntPtr.Add(table, 4 + index * Marshal.SizeOf<TcpRow>()));
                if ((ushort)IPAddress.NetworkToHostOrder((short)row.LocalPort) != port) continue;
                if (row.LocalAddress != BitConverter.ToUInt32(IPAddress.Loopback.GetAddressBytes())) throw new InvalidOperationException("CDP listener is not restricted to IPv4 loopback.");
                return row.ProcessId;
            }
            return null;
        }
        finally { Marshal.FreeHGlobal(table); }
    }
    public void Dispose() => handle.Dispose();
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string? Reserved, Desktop, Title;
        public uint X, Y, Width, Height, CharacterWidth, CharacterHeight, FillAttribute, Flags;
        public ushort ShowWindow, ReservedSize;
        public IntPtr ReservedBytes, StandardInput, StandardOutput, StandardError;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process, Thread;
        public uint ProcessId, ThreadId;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct TcpRow { public uint State, LocalAddress, LocalPort, RemoteAddress, RemotePort, ProcessId; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Accounting
    {
        public long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime;
        public uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long ProcessTime, JobTime;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSet, MaximumWorkingSet;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
        public UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateJobObject(IntPtr attributes, string? name);
    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(string application, StringBuilder commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment, string directory,
        ref StartupInfo startup, out ProcessInformation information);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(SafeFileHandle thread);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(SafeFileHandle process, uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeFileHandle process, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(SafeFileHandle job, int infoClass, ref ExtendedLimits info, int length);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsProcessInJob(IntPtr process, SafeFileHandle job, out bool result);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryInformationJobObject(SafeFileHandle job, int infoClass, out Accounting info, int length, IntPtr returnedLength);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateJobObject(SafeFileHandle job, uint exitCode);
    [DllImport("iphlpapi.dll")]
    private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int family, int tableClass, uint reserved);
}
