using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Messages;

namespace Perp.Nexus.Core.Sagas;

public interface ISagaOrchestrator
{
    Task HandleAsync<T>(ConsumeContext<T> context, CancellationToken cancellationToken = default);
}

public abstract class SagaOrchestrator<TState> : ISagaOrchestrator where TState : SagaState
{
    protected readonly ISagaStore Store;

    protected SagaOrchestrator(ISagaStore store)
    {
        Store = store;
    }

    public async Task HandleAsync<T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
    {
        var correlationId = context.Envelope.CorrelationId;
        var state = await Store.LoadAsync<TState>(correlationId, cancellationToken);

        state ??= CreateInitialState(correlationId);

        state.LastEvent = typeof(T).FullName;
        await TransitionAsync(state, context, cancellationToken);
        state.LastModified = DateTime.UtcNow;
        state.Version++;

        if (state.CurrentState is "Completed" or "Failed" or "Cancelled")
            state.CompletedAt = DateTime.UtcNow;

        await Store.SaveAsync(state, cancellationToken);
    }

    protected abstract TState CreateInitialState(Guid correlationId);

    protected abstract Task TransitionAsync<T>(TState state, ConsumeContext<T> context, CancellationToken cancellationToken);
}
