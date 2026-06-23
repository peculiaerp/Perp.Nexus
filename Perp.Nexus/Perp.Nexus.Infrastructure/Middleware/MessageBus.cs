using System.Text.Json;
using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Middleware;
using Perp.Nexus.Core.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class MessageBus : IBus
{
    private readonly IMessageTransport _transport;
    private readonly MiddlewarePipeline _publishPipeline;
    private readonly MiddlewarePipeline _sendPipeline;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageBus> _logger;

    public MessageBus(
        IMessageTransport transport,
        IEnumerable<IMiddleware> middlewares,
        IServiceProvider serviceProvider,
        ILogger<MessageBus> logger)
    {
        _transport = transport;
        _serviceProvider = serviceProvider;
        _logger = logger;

        var middlewareList = middlewares.ToList();
        _publishPipeline = new MiddlewarePipeline(middlewareList);
        _sendPipeline = new MiddlewarePipeline(middlewareList);
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = CreateEnvelope(message, "Publish");
        var context = new MiddlewareContext
        {
            Envelope = envelope,
            Action = "Publish",
            ServiceProvider = _serviceProvider,
            CancellationToken = cancellationToken
        };

        await _publishPipeline.ExecuteAsync(context);
        await _transport.PublishAsync(envelope, cancellationToken);

        _logger.LogInformation("Published {Type} [{MessageId}]", envelope.Type, envelope.MessageId);
    }

    public async Task SendAsync<T>(T message, string destination, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = CreateEnvelope(message, "Send");
        envelope = envelope.WithHeader("destination", destination);

        var context = new MiddlewareContext
        {
            Envelope = envelope,
            Action = "Send",
            ServiceProvider = _serviceProvider,
            CancellationToken = cancellationToken
        };

        await _sendPipeline.ExecuteAsync(context);
        await _transport.SendAsync(envelope, cancellationToken);

        _logger.LogInformation("Sent {Type} [{MessageId}] to {Destination}", envelope.Type, envelope.MessageId, destination);
    }

    public async Task PublishWithCorrelationAsync<T>(T message, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = CreateEnvelope(message, "Publish");
        envelope = envelope.WithCorrelationId(correlationId);
        if (causationId.HasValue)
            envelope = envelope.WithCausationId(causationId.Value);

        var context = new MiddlewareContext
        {
            Envelope = envelope,
            Action = "Publish",
            ServiceProvider = _serviceProvider,
            CancellationToken = cancellationToken
        };

        await _publishPipeline.ExecuteAsync(context);
        await _transport.PublishAsync(envelope, cancellationToken);
        _logger.LogInformation("Published {Type} [{MessageId}] with correlation {CorrelationId}", envelope.Type, envelope.MessageId, correlationId);
    }

    public async Task SendWithCorrelationAsync<T>(T message, string destination, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = CreateEnvelope(message, "Send");
        envelope = envelope.WithCorrelationId(correlationId).WithHeader("destination", destination);
        if (causationId.HasValue)
            envelope = envelope.WithCausationId(causationId.Value);

        var context = new MiddlewareContext
        {
            Envelope = envelope,
            Action = "Send",
            ServiceProvider = _serviceProvider,
            CancellationToken = cancellationToken
        };

        await _sendPipeline.ExecuteAsync(context);
        await _transport.SendAsync(envelope, cancellationToken);
        _logger.LogInformation("Sent {Type} [{MessageId}] with correlation {CorrelationId} to {Destination}", envelope.Type, envelope.MessageId, correlationId, destination);
    }

    private static EventEnvelope CreateEnvelope<T>(T message, string action) where T : class
    {
        return new EventEnvelope
        {
            MessageId = Guid.NewGuid(),
            Type = typeof(T).FullName!,
            Version = 1,
            Payload = JsonSerializer.Serialize(message),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };
    }
}
