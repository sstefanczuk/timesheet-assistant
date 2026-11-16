namespace TimesheetAssistant.Core.Features.Workdays;

/// <summary>Operations are atomic. Failures throw WorkEntryStoreException without changing committed data.</summary>
public interface IWorkEntryStore
{
    /// <summary>Returns only this date's entries, ordered by immutable creation order.</summary>
    Task<IReadOnlyList<WorkEntry>> ReadAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Returns complete nonempty days, newest first, from one consistent read snapshot.</summary>
    Task<WorkEntryHistoryPage> ReadHistoryAsync(DateOnly beforeExclusive, int dayLimit, CancellationToken cancellationToken = default);

    /// <summary>Returns all nonempty days in the inclusive/exclusive interval from one consistent read snapshot.</summary>
    Task<WorkEntryHistoryPage> ReadHistoryRangeAsync(DateOnly fromInclusive, DateOnly beforeExclusive, CancellationToken cancellationToken = default);

    /// <summary>Assigns a unique identity and monotonically increasing creation order.</summary>
    Task<WorkEntry> AddAsync(DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default);

    /// <summary>Returns null if identity/date does not match. Preserves identity, date and creation order.</summary>
    Task<WorkEntry?> UpdateAsync(Guid id, DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default);

    /// <summary>Returns false if identity/date does not match.</summary>
    Task<bool> DeleteAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default);
}

public sealed class WorkEntryStoreException(string message, Exception? innerException = null)
    : Exception(message, innerException);
