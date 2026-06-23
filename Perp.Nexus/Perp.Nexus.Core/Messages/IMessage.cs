namespace Perp.Nexus.Core.Messages;

public interface IMessage
{
    Guid MessageId { get; }
    string Type { get; }
    int Version { get; }
    string Payload { get; }
    Guid CorrelationId { get; }
    Guid? CausationId { get; }
    DateTime Timestamp { get; }
}
