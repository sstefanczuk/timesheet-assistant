namespace TimesheetAssistant.Features.Workdays;

internal sealed class DataDirectoryLease : IDisposable
{
    private readonly FileStream _handle;

    private DataDirectoryLease(FileStream handle)
    {
        _handle = handle;
    }

    public static DataDirectoryLease Acquire(string directory)
    {
        try
        {
            // Never delete this file: every process must contend for the same filesystem object.
            var handle = new FileStream(Path.Combine(directory, "timesheet.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            return new DataDirectoryLease(handle);
        }
        catch (IOException exception) when ((exception.HResult & 0xffff) is 32 or 33)
        {
            throw new StorageInitializationException(
                "Another Timesheet Assistant instance is using this data directory. Close it, then select Retry.", exception);
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
    }
}
