using System.Text.Json;
using Perp.Nexus.Core.Sagas;
using StackExchange.Redis;

namespace Perp.Nexus.Infrastructure.Sagas;

internal sealed class CachedSagaStore : ISagaStore
{
    private readonly ISagaStore _innerStore;
    private readonly IDatabase _redis;
    private readonly TimeSpan _cacheTtl;

    public CachedSagaStore(ISagaStore innerStore, IConnectionMultiplexer redis, TimeSpan? cacheTtl = null)
    {
        _innerStore = innerStore;
        _redis = redis.GetDatabase();
        _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);
    }

    private static string CacheKey<TState>(Guid correlationId) where TState : SagaState
        => $"saga:{typeof(TState).FullName}:{correlationId}";

    public async Task<TState?> LoadAsync<TState>(Guid correlationId, CancellationToken cancellationToken = default) where TState : SagaState
    {
        var cached = await _redis.StringGetAsync(CacheKey<TState>(correlationId));
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<TState>((string)cached!);
        }

        var state = await _innerStore.LoadAsync<TState>(correlationId, cancellationToken);
        if (state != null)
        {
            await _redis.StringSetAsync(CacheKey<TState>(correlationId), (string?)JsonSerializer.Serialize(state), _cacheTtl);
        }
        return state;
    }

    public async Task SaveAsync<TState>(TState state, CancellationToken cancellationToken = default) where TState : SagaState
    {
        await _innerStore.SaveAsync(state, cancellationToken);
        await _redis.StringSetAsync(CacheKey<TState>(state.CorrelationId), (string?)JsonSerializer.Serialize(state), _cacheTtl);
    }

    public async Task DeleteAsync<TState>(Guid correlationId, CancellationToken cancellationToken = default) where TState : SagaState
    {
        await _innerStore.DeleteAsync<TState>(correlationId, cancellationToken);
        await _redis.KeyDeleteAsync(CacheKey<TState>(correlationId));
    }
}
