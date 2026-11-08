using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
            using var launchToken = GetStandardLaunchToken(out var restrictedToken);
            ProcessInformation info;
            var created = launchToken is null
                ? CreateProcess(start.FileName, new StringBuilder($"\"{start.FileName}\""), IntPtr.Zero, IntPtr.Zero,
                    false, createSuspended | createUnicodeEnvironment, environmentBlock, start.WorkingDirectory, ref startup, out info)
                : restrictedToken
                    // An own restricted primary token does not require SeAssignPrimaryTokenPrivilege.
                    ? CreateProcessAsUser(launchToken, start.FileName, new StringBuilder($"\"{start.FileName}\""), IntPtr.Zero, IntPtr.Zero,
                        false, createSuspended | createUnicodeEnvironment, environmentBlock, start.WorkingDirectory, ref startup, out info)
                    : CreateProcessWithToken(launchToken, 0, start.FileName, new StringBuilder($"\"{start.FileName}\""),
                        createSuspended | createUnicodeEnvironment, environmentBlock, start.WorkingDirectory, ref startup, out info);
            if (!created)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create suspended desktop process.");

            using var processHandle = new SafeFileHandle(info.Process, ownsHandle: true);
            using var threadHandle = new SafeFileHandle(info.Thread, ownsHandle: true);
            Process? process = null;
            try
            {
                // The primary thread cannot create WebView2 children before job membership is established.
                if (!AssignProcessToJobObject(handle, processHandle.DangerousGetHandle()))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not isolate suspended desktop process in a Windows job.");
                if (launchToken is not null)
                {
                    if (!OpenProcessToken(processHandle.DangerousGetHandle(), 0x0008, out var childToken))
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not verify desktop child token.");
                    using (childToken) VerifyStandardToken(childToken);
                }
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

    private static SafeAccessTokenHandle? GetStandardLaunchToken(out bool restrictedToken)
    {
        restrictedToken = false;
        // Current WebView2 ignores environment overrides in elevated hosts. De-elevate only our child.
        const uint tokenAccess = 0x0001 | 0x0002 | 0x0008 | 0x0080; // assign, duplicate, query, adjust default
        using var currentProcess = Process.GetCurrentProcess();
        if (!OpenProcessToken(currentProcess.Handle, tokenAccess, out var current))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not inspect desktop launch token.");
        using (current)
        {
            if (ReadTokenInt(current, 20) == 0 && ReadIntegrity(current) < 0x3000) return null;
            SafeAccessTokenHandle token;
            if (ReadTokenInt(current, 18) == 2) // TokenElevationTypeFull has a linked filtered token.
            {
                var linked = ReadTokenInformation(current, 19);
                try { token = new SafeAccessTokenHandle(Marshal.ReadIntPtr(linked)); }
                finally { Marshal.FreeHGlobal(linked); }
            }
            else
            {
                // UAC-disabled hosts have no linked token. LUA_TOKEN strips admin groups/privileges.
                if (!CreateRestrictedToken(current, 0x4 | 0x1, 0, IntPtr.Zero, 0, IntPtr.Zero, 0, IntPtr.Zero, out token))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create standard desktop launch token.");
                restrictedToken = true;
            }
            try
            {
                if (ReadIntegrity(token) != 0x2000)
                {
                    var mediumSid = new SecurityIdentifier("S-1-16-8192");
                    var bytes = new byte[mediumSid.BinaryLength];
                    mediumSid.GetBinaryForm(bytes, 0);
                    var sid = Marshal.AllocHGlobal(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, sid, bytes.Length);
                        var label = new MandatoryLabel { Sid = sid, Attributes = 0x20 }; // SE_GROUP_INTEGRITY
                        if (!SetTokenInformation(token, 25, ref label, Marshal.SizeOf<MandatoryLabel>() + bytes.Length))
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set medium desktop integrity.");
                    }
                    finally { Marshal.FreeHGlobal(sid); }
                }
                VerifyStandardToken(token);
                return token;
            }
            catch { token.Dispose(); throw; }
        }
    }

    private static void VerifyStandardToken(SafeAccessTokenHandle token)
    {
        if (ReadTokenInt(token, 20) != 0 || ReadIntegrity(token) != 0x2000)
            throw new InvalidOperationException("Desktop test child must have a non-elevated medium-integrity token so WebView2 honors its private profile and CDP arguments.");
    }

    private static int ReadIntegrity(SafeAccessTokenHandle token)
    {
        var data = ReadTokenInformation(token, 25);
        try
        {
            var sid = new SecurityIdentifier(Marshal.ReadIntPtr(data));
            return int.Parse(sid.Value.Split('-')[^1], System.Globalization.CultureInfo.InvariantCulture);
        }
        finally { Marshal.FreeHGlobal(data); }
    }

    private static int ReadTokenInt(SafeAccessTokenHandle token, int informationClass)
    {
        var data = ReadTokenInformation(token, informationClass);
        try { return Marshal.ReadInt32(data); }
        finally { Marshal.FreeHGlobal(data); }
    }

    private static IntPtr ReadTokenInformation(SafeAccessTokenHandle token, int informationClass)
    {
        GetTokenInformation(token, informationClass, IntPtr.Zero, 0, out var size);
        if (size == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not size desktop token information.");
        var data = Marshal.AllocHGlobal(size);
        if (GetTokenInformation(token, informationClass, data, size, out _)) return data;
        var error = Marshal.GetLastWin32Error();
        Marshal.FreeHGlobal(data);
        throw new Win32Exception(error, "Could not read desktop token information.");
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
    private struct MandatoryLabel { public IntPtr Sid; public uint Attributes; }
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, IntPtr data, int length, out int needed);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetTokenInformation(SafeAccessTokenHandle token, int informationClass, ref MandatoryLabel label, int length);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CreateRestrictedToken(SafeAccessTokenHandle token, uint flags, uint disabledCount, IntPtr disabled,
        uint deletedCount, IntPtr deleted, uint restrictedCount, IntPtr restricted, out SafeAccessTokenHandle result);
    [DllImport("advapi32.dll", EntryPoint = "CreateProcessWithTokenW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessWithToken(SafeAccessTokenHandle token, uint logonFlags, string application,
        StringBuilder commandLine, uint flags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInformation information);
    [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUserW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessAsUser(SafeAccessTokenHandle token, string application, StringBuilder commandLine,
        IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment,
        string directory, ref StartupInfo startup, out ProcessInformation information);
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
