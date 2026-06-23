namespace Perp.Nexus.Core.Outbox;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}

public sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public Guid CorrelationId { get; init; }
    public string? Destination { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedOn { get; set; }
    public bool IsProcessed { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
