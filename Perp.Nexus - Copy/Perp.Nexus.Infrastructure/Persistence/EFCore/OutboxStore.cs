using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.Outbox;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

internal sealed class OutboxStore : IOutboxStore
{
    private readonly MessagingDbContext _db;

    public OutboxStore(MessagingDbContext db) => _db = db;

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _db.OutboxMessages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        return await _db.OutboxMessages
            .Where(x => !x.IsProcessed)
            .OrderBy(x => x.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.OutboxMessages.FindAsync([id], cancellationToken);
        if (message != null)
        {
            message.IsProcessed = true;
            message.ProcessedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.OutboxMessages.CountAsync(x => !x.IsProcessed, cancellationToken);
    }
}
