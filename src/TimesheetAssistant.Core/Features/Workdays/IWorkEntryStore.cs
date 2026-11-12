namespace TimesheetAssistant.Core.Features.Workdays;

/// <summary>Operations are atomic. Failures throw WorkEntryStoreException without changing committed data.</summary>
public interface IWorkEntryStore
{
    /// <summary>Returns only this date's entries, ordered by immutable creation order.</summary>
    Task<IReadOnlyList<WorkEntry>> ReadAsync(DateOnly date, CancellationToken cancellationToken = default);
    /// <summary>Assigns a unique identity and monotonically increasing creation order.</summary>
    Task<WorkEntry> AddAsync(DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default);
    /// <summary>Returns null if identity/date does not match. Preserves identity, date and creation order.</summary>
    Task<WorkEntry?> UpdateAsync(Guid id, DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default);
    /// <summary>Returns false if identity/date does not match.</summary>
    Task<bool> DeleteAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default);
}

public sealed class WorkEntryStoreException(string message, Exception? innerException = null)
    : Exception(message, innerException);
