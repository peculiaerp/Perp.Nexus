namespace Perp.Nexus.Core.Sagas;

public interface ISaga
{
    Guid CorrelationId { get; }
    string CurrentState { get; }
}

public abstract class SagaState
{
    public required Guid CorrelationId { get; init; }
    public string CurrentState { get; set; } = "Initial";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int Version { get; set; } = 1;
    public string? LastEvent { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
