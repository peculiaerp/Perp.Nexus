using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.Inbox;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

internal sealed class InboxStore : IInboxStore
{
    private readonly MessagingDbContext _db;

    public InboxStore(MessagingDbContext db) => _db = db;

    public async Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _db.InboxMessages.AnyAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default)
    {
        await _db.InboxMessages.AddAsync(new InboxMessage
        {
            MessageId = messageId,
            MessageType = messageType,
            ReceivedAt = DateTime.UtcNow
        }, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var old = await _db.InboxMessages
            .Where(x => x.ReceivedAt < cutoff)
            .ToListAsync(cancellationToken);
        _db.InboxMessages.RemoveRange(old);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
