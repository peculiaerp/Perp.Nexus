using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.DeadLetter;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

internal sealed class DeadLetterStore : IDeadLetterStore
{
    private readonly MessagingDbContext _db;

    public DeadLetterStore(MessagingDbContext db) => _db = db;

    public async Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        await _db.DeadLetterMessages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DeadLetterMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DeadLetterMessages
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.DeadLetterMessages.FindAsync([id], cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DeadLetterMessages.CountAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.DeadLetterMessages.FindAsync([id], cancellationToken);
        if (message != null)
        {
            _db.DeadLetterMessages.Remove(message);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
