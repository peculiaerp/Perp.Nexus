namespace Perp.Nexus.Core.Sagas;

public interface ISagaStore
{
    Task<TState?> LoadAsync<TState>(Guid correlationId, CancellationToken cancellationToken = default) where TState : SagaState;
    Task SaveAsync<TState>(TState state, CancellationToken cancellationToken = default) where TState : SagaState;
    Task DeleteAsync<TState>(Guid correlationId, CancellationToken cancellationToken = default) where TState : SagaState;
}
