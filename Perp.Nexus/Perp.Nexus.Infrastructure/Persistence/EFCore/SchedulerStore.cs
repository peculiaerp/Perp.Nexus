using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.Scheduling;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

internal sealed class SchedulerStore : ISchedulerStore
{
    private readonly MessagingDbContext _db;

    public SchedulerStore(MessagingDbContext db) => _db = db;

    public async Task AddAsync(ScheduledMessage message, CancellationToken cancellationToken = default)
    {
        await _db.ScheduledMessages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ScheduledMessage>> GetDueMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ScheduledMessages
            .Where(x => !x.IsExecuted && x.ExecuteAt <= DateTime.UtcNow)
            .OrderBy(x => x.ExecuteAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.ScheduledMessages.FindAsync([id], cancellationToken);
        if (message != null)
        {
            _db.ScheduledMessages.Remove(message);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ScheduledMessages.CountAsync(x => !x.IsExecuted, cancellationToken);
    }
}
