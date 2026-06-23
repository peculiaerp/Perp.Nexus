namespace OrderSystem.Commands;

public sealed record CreateOrder(
    string CustomerId,
    List<OrderItem> Items);

public sealed record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record ProcessPayment(
    Guid OrderId,
    decimal Amount,
    string PaymentMethod);

public sealed record ReserveInventory(
    Guid OrderId,
    List<string> ProductIds);

public sealed record ShipOrder(
    Guid OrderId,
    string Carrier);
