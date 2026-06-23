using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Sagas;
using OrderSystem.Events;
using OrderSystem.Sagas;

namespace OrderSystem.Consumers;

public sealed class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ISagaOrchestrator _orchestrator;

    public OrderCreatedConsumer(ISagaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task ConsumeAsync(ConsumeContext<OrderCreated> context, CancellationToken cancellationToken = default)
    {
        await _orchestrator.HandleAsync(context, cancellationToken);
    }
}

public sealed class PaymentReceivedConsumer : IConsumer<PaymentReceived>
{
    private readonly ISagaOrchestrator _orchestrator;

    public PaymentReceivedConsumer(ISagaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task ConsumeAsync(ConsumeContext<PaymentReceived> context, CancellationToken cancellationToken = default)
    {
        await _orchestrator.HandleAsync(context, cancellationToken);
    }
}

public sealed class InventoryReservedConsumer : IConsumer<InventoryReserved>
{
    private readonly ISagaOrchestrator _orchestrator;

    public InventoryReservedConsumer(ISagaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task ConsumeAsync(ConsumeContext<InventoryReserved> context, CancellationToken cancellationToken = default)
    {
        await _orchestrator.HandleAsync(context, cancellationToken);
    }
}

public sealed class OrderShippedConsumer : IConsumer<OrderShipped>
{
    private readonly ISagaOrchestrator _orchestrator;

    public OrderShippedConsumer(ISagaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task ConsumeAsync(ConsumeContext<OrderShipped> context, CancellationToken cancellationToken = default)
    {
        await _orchestrator.HandleAsync(context, cancellationToken);
    }
}
