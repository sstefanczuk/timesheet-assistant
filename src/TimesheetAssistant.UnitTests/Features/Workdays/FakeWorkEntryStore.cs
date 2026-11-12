using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

internal sealed class FakeWorkEntryStore : IWorkEntryStore
{
    private readonly List<WorkEntry> _entries = [];

    private long _nextOrder;

    public bool Fail { get; set; }

    public List<WorkEntry>? LastRead { get; private set; }

    public int Calls { get; private set; }

    public Task<IReadOnlyList<WorkEntry>> ReadAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        BeforeOperation();

        LastRead = _entries.Where(e => e.WorkDate == date).OrderBy(e => e.CreationOrder).ToList();

        return Task.FromResult<IReadOnlyList<WorkEntry>>(LastRead);
    }

    public Task<WorkEntry> AddAsync(DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default)
    {
        BeforeOperation();

        var entry = new WorkEntry(Guid.NewGuid(), date, values.JiraId, values.Title, values.Description, values.DurationMinutes, ++_nextOrder);
        _entries.Add(entry);

        return Task.FromResult(entry);
    }

    public Task<WorkEntry?> UpdateAsync(Guid id, DateOnly date, WorkEntryValues values, CancellationToken cancellationToken = default)
    {
        BeforeOperation();

        var index = _entries.FindIndex(e => e.Id == id && e.WorkDate == date);

        if (index < 0)
        {
            return Task.FromResult<WorkEntry?>(null);
        }

        var entry = _entries[index] with
        {
            JiraId = values.JiraId,
            Title = values.Title,
            Description = values.Description,
            DurationMinutes = values.DurationMinutes
        };

        _entries[index] = entry;

        return Task.FromResult<WorkEntry?>(entry);
    }

    public Task<bool> DeleteAsync(Guid id, DateOnly date, CancellationToken cancellationToken = default)
    {
        BeforeOperation();

        return Task.FromResult(_entries.RemoveAll(e => e.Id == id && e.WorkDate == date) == 1);
    }

    private void BeforeOperation()
    {
        Calls++;

        if (Fail)
        {
            throw new WorkEntryStoreException("Simulated storage failure.");
        }
    }
}
