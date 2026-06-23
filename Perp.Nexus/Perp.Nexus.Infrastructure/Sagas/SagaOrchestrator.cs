using System.Text.Json;
using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Sagas;

namespace Perp.Nexus.Infrastructure.Sagas;

internal sealed class SagaOrchestrator<TState> : ISagaOrchestrator where TState : SagaState
{
    private readonly ISagaStore _store;
    private readonly Func<Guid, TState> _stateFactory;
    private readonly Func<TState, object, Task<string>> _transitionHandler;

    public SagaOrchestrator(
        ISagaStore store,
        Func<Guid, TState> stateFactory,
        Func<TState, object, Task<string>> transitionHandler)
    {
        _store = store;
        _stateFactory = stateFactory;
        _transitionHandler = transitionHandler;
    }

    public async Task HandleAsync<T>(ConsumeContext<T> context, CancellationToken cancellationToken = default)
    {
        var correlationId = context.Envelope.CorrelationId;
        var state = await _store.LoadAsync<TState>(correlationId, cancellationToken);

        state ??= _stateFactory(correlationId);

        state.LastEvent = typeof(T).FullName;
        var newState = await _transitionHandler(state, context.Message!);
        state.CurrentState = newState;
        state.LastModified = DateTime.UtcNow;
        state.Version++;

        if (newState is "Completed" or "Failed" or "Cancelled")
        {
            state.CompletedAt = DateTime.UtcNow;
        }

        await _store.SaveAsync(state, cancellationToken);
    }

    public static SagaOrchestrator<TState> Create(
        ISagaStore store,
        Func<Guid, TState> stateFactory,
        Func<TState, object, Task<string>> transitionHandler)
        => new(store, stateFactory, transitionHandler);
}
