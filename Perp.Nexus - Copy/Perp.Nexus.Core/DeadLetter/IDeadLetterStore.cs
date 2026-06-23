namespace Perp.Nexus.Core.DeadLetter;

public interface IDeadLetterStore
{
    Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
    Task<List<DeadLetterMessage>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class DeadLetterMessage
{
    public required Guid Id { get; init; }
    public required string MessageType { get; init; }
    public required string Payload { get; init; }
    public required string ErrorReason { get; init; }
    public string? StackTrace { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int RetryCount { get; init; }
    public string? ExceptionType { get; init; }
}
