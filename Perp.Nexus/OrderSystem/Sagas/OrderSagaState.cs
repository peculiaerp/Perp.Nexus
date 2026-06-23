using Perp.Nexus.Core.Sagas;

namespace OrderSystem.Sagas;

public sealed class OrderSagaState : SagaState
{
    public string? CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentMethod { get; set; }
    public List<string> ReservedItems { get; set; } = new();
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? FailureReason { get; set; }
}
