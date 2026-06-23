using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.Sagas;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

internal sealed class SagaStore : ISagaStore
{
    private readonly MessagingDbContext _db;

    public SagaStore(MessagingDbContext db) => _db = db;

    public async Task<TState?> LoadAsync<TState>(Guid correlationId, CancellationToken cancellationToken = default) where TState : SagaState
    {
        var entity = await _db.SagaStates
            .FirstOrDefaultAsync(x => x.CorrelationId == correlationId && x.SagaType == typeof(TState).FullName, cancellationToken);

        if (entity?.StateData == null) return null;

        var state = JsonSerializer.Deserialize<TState>(entity.StateData);
        if (state != null)
        {
            state.CurrentState = entity.CurrentState;
            state.Version = entity.Version;
        }
        return state;
    }

    public async Task SaveAsync<TState>(TState state, CancellationToken cancellationToken = default) where TState : SagaState
    {
        var entity = await _db.SagaStates
            .FirstOrDefaultAsync(x => x.CorrelationId == state.CorrelationId && x.SagaType == typeof(TState).FullName, cancellationToken);

        if (entity == null)
        {
            entity = new SagaStateEntity
            {
                CorrelationId = state.CorrelationId,
                SagaType = typeof(TState).FullName!,
                CurrentState = state.CurrentState,
                StateData = JsonSerializer.Serialize(state),
                CreatedAt = state.CreatedAt,
                Version = state.Version
            };
            _db.SagaStates.Add(entity);
        }
        else
        {
            entity.CurrentState = state.CurrentState;
            entity.StateData = JsonSerializer.Serialize(state);
            entity.LastModified = DateTime.UtcNow;
            entity.Version = state.Version;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync<TState>(Guid correlationId, CancellationToken cancellationToken = default) where TState : SagaState
    {
        var entity = await _db.SagaStates
            .FirstOrDefaultAsync(x => x.CorrelationId == correlationId && x.SagaType == typeof(TState).FullName, cancellationToken);
        if (entity != null)
        {
            _db.SagaStates.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
