namespace OrderSystem.Events;

public sealed record OrderCreated(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    List<OrderItem> Items);

public sealed record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record PaymentReceived(
    Guid OrderId,
    string TransactionId,
    decimal Amount,
    string PaymentMethod);

public sealed record InventoryReserved(
    Guid OrderId,
    List<string> ReservedItems,
    bool Success);

public sealed record OrderShipped(
    Guid OrderId,
    string TrackingNumber,
    string Carrier);

public sealed record OrderCompleted(
    Guid OrderId,
    DateTime CompletedAt);

public sealed record OrderFailed(
    Guid OrderId,
    string Reason);
