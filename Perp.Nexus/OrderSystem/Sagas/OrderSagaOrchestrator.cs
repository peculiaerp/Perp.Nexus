using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Sagas;
using OrderSystem.Events;

namespace OrderSystem.Sagas;

public sealed class OrderSagaOrchestrator : SagaOrchestrator<OrderSagaState>
{
    private readonly IBus _bus;

    public OrderSagaOrchestrator(ISagaStore store, IBus bus) : base(store)
    {
        _bus = bus;
    }

    protected override OrderSagaState CreateInitialState(Guid correlationId)
        => new() { CorrelationId = correlationId };

    protected override async Task TransitionAsync<T>(OrderSagaState state, ConsumeContext<T> context, CancellationToken cancellationToken)
    {
        switch (context.Message)
        {
            case OrderCreated created:
                await HandleOrderCreated(state, created, context.Envelope.CorrelationId, cancellationToken);
                break;
            case PaymentReceived payment:
                await HandlePaymentReceived(state, payment, context.Envelope.CorrelationId, cancellationToken);
                break;
            case InventoryReserved inventory:
                await HandleInventoryReserved(state, inventory, context.Envelope.CorrelationId, cancellationToken);
                break;
            case OrderShipped shipped:
                await HandleOrderShipped(state, shipped, context.Envelope.CorrelationId, cancellationToken);
                break;
        }
    }

    private async Task HandleOrderCreated(OrderSagaState state, OrderCreated evt, Guid correlationId, CancellationToken ct)
    {
        state.CustomerId = evt.CustomerId;
        state.TotalAmount = evt.TotalAmount;
        state.CurrentState = "AwaitingPayment";

        await _bus.PublishWithCorrelationAsync(
            new PaymentReceived(evt.OrderId, Guid.NewGuid().ToString("N"), evt.TotalAmount, "CreditCard"),
            correlationId, null, ct);
    }

    private async Task HandlePaymentReceived(OrderSagaState state, PaymentReceived evt, Guid correlationId, CancellationToken ct)
    {
        state.TransactionId = evt.TransactionId;
        state.PaymentMethod = evt.PaymentMethod;
        state.CurrentState = "AwaitingInventory";

        await _bus.PublishWithCorrelationAsync(
            new InventoryReserved(Guid.Parse(correlationId.ToString()), new List<string>(), true),
            correlationId, null, ct);
    }

    private async Task HandleInventoryReserved(OrderSagaState state, InventoryReserved evt, Guid correlationId, CancellationToken ct)
    {
        if (!evt.Success)
        {
            state.CurrentState = "Failed";
            state.FailureReason = "Inventory reservation failed";
            await _bus.PublishWithCorrelationAsync(
                new OrderFailed(Guid.Parse(correlationId.ToString()), state.FailureReason),
                correlationId, null, ct);
            return;
        }

        state.ReservedItems = evt.ReservedItems;
        state.CurrentState = "AwaitingShipment";

        await _bus.PublishWithCorrelationAsync(
            new OrderShipped(Guid.Parse(correlationId.ToString()), "TRACK-" + Guid.NewGuid().ToString("N")[..8], "FedEx"),
            correlationId, null, ct);
    }

    private async Task HandleOrderShipped(OrderSagaState state, OrderShipped evt, Guid correlationId, CancellationToken ct)
    {
        state.TrackingNumber = evt.TrackingNumber;
        state.Carrier = evt.Carrier;
        state.CurrentState = "Completed";
        state.CompletedAt = DateTime.UtcNow;

        await _bus.PublishWithCorrelationAsync(
            new OrderCompleted(Guid.Parse(correlationId.ToString()), DateTime.UtcNow),
            correlationId, null, ct);
    }
}
