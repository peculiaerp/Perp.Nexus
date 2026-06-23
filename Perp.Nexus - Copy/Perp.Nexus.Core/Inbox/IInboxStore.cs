namespace Perp.Nexus.Core.Inbox;

public interface IInboxStore
{
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default);
    Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}

public sealed class InboxMessage
{
    public required Guid MessageId { get; init; }
    public required string MessageType { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
