using System.Collections.Concurrent;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Transports;

namespace Perp.Nexus.Infrastructure.Transports;

internal sealed class InMemoryTransport : IMessageTransport
{
    private readonly ConcurrentDictionary<string, List<Func<EventEnvelope, Task>>> _subscribers = new();

    public string Name => "InMemory";

    public Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
        => PublishAsync(envelope, cancellationToken);

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (_subscribers.TryGetValue(envelope.Type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(envelope);
            }
        }
    }

    public Task SubscribeAsync(string topic, Func<EventEnvelope, Task> handler, CancellationToken cancellationToken = default)
    {
        _subscribers.AddOrUpdate(topic, _ => [handler], (_, list) => { list.Add(handler); return list; });
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string topic)
    {
        _subscribers.TryRemove(topic, out _);
        return Task.CompletedTask;
    }
}
