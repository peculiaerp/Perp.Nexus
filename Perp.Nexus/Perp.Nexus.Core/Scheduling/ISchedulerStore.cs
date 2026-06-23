namespace Perp.Nexus.Core.Scheduling;

public interface ISchedulerStore
{
    Task AddAsync(ScheduledMessage message, CancellationToken cancellationToken = default);
    Task<List<ScheduledMessage>> GetDueMessagesAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}

public sealed class ScheduledMessage
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public Guid CorrelationId { get; init; }
    public required DateTime ExecuteAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsExecuted { get; set; }
    public DateTime? ExecutedAt { get; set; }
}
