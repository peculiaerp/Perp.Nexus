using Perp.Nexus.Core.Messages;

namespace Perp.Nexus.Core.Transports;

public interface IMessageTransport
{
    string Name { get; }
    Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string topic, Func<EventEnvelope, Task> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string topic);
}

public enum TransportType
{
    RabbitMQ,
    Kafka,
    InMemory
}
