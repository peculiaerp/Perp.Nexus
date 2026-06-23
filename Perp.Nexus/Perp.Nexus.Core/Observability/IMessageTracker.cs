using Perp.Nexus.Core.Messages;

namespace Perp.Nexus.Core.Observability;

public interface IMessageTracker
{
    void TrackPublish(EventEnvelope envelope);
    void TrackConsume(EventEnvelope envelope, string consumerType, long elapsedMs);
    void TrackSagaEvent(EventEnvelope envelope, string sagaType, string state);
    void TrackDeadLetter(EventEnvelope envelope, string reason);
    void TrackRetry(EventEnvelope envelope, int attempt, string consumerType);
}

public sealed class MessageTraceEvent
{
    public required string EventType { get; init; }
    public required Guid MessageId { get; init; }
    public required string MessageType { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; init; } = new();
}
