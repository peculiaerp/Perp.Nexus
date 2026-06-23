namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

public sealed class SagaStateEntity
{
    public required Guid CorrelationId { get; init; }
    public required string SagaType { get; init; }
    public string CurrentState { get; set; } = "Initial";
    public string? StateData { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}
