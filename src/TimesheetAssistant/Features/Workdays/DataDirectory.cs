using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace TimesheetAssistant.Features.Workdays;

internal static class DataDirectory
{
    public const string OverrideVariable = "TIMESHEET_ASSISTANT_DATA_DIRECTORY";

    public static string Resolve()
    {
        var configured = Environment.GetEnvironmentVariable(OverrideVariable);
        var path = configured ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimesheetAssistant");

        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            throw new StorageInitializationException("The configured data directory must be an absolute path.");
        }

        path = Path.GetFullPath(path);
        Directory.CreateDirectory(path);

        // Resolve junctions and directory aliases before choosing the database and lease paths.
        using var handle = CreateFile(path, 0, FileShare.ReadWrite | FileShare.Delete,
            IntPtr.Zero, FileMode.Open, 0x02000000, IntPtr.Zero);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var buffer = new StringBuilder(32768);
        var length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 0);

        if (length == 0 || length >= buffer.Capacity)
        {
            throw new IOException("The data directory could not be resolved.");
        }

        var resolved = buffer.ToString();

        if (resolved.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + resolved[8..];
        }

        return resolved.StartsWith(@"\\?\", StringComparison.Ordinal) ? resolved[4..] : resolved;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess,
        FileShare shareMode, IntPtr securityAttributes, FileMode creationDisposition,
        uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle file,
        StringBuilder filePath, uint filePathSize, uint flags);
}
