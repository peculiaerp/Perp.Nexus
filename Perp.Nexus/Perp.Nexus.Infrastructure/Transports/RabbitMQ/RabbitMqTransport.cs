using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Transports;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Perp.Nexus.Infrastructure.Transports.RabbitMQ;

internal sealed class RabbitMqTransport : IMessageTransport, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchange;
    private readonly ILogger<RabbitMqTransport> _logger;

    public RabbitMqTransport(string connectionString, string exchange = "masstransist", ILogger<RabbitMqTransport>? logger = null)
    {
        _exchange = exchange;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RabbitMqTransport>.Instance;
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
    }

    public string Name => "RabbitMQ";

    public async Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var body = Serialize(envelope);
        var props = new BasicProperties { Persistent = true };
        var routingKey = envelope.Headers.GetValueOrDefault("routingKey", envelope.Type);
        await _channel.BasicPublishAsync(
            exchange: _exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
        _logger.LogDebug("Sent {Type} to RabbitMQ exchange {Exchange}", envelope.Type, _exchange);
    }

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var body = Serialize(envelope);
        var props = new BasicProperties { Persistent = true };
        await _channel.BasicPublishAsync(
            exchange: _exchange,
            routingKey: envelope.Type,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
        _logger.LogDebug("Published {Type} to RabbitMQ", envelope.Type);
    }

    public async Task SubscribeAsync(string topic, Func<EventEnvelope, Task> handler, CancellationToken cancellationToken = default)
    {
        var queueName = await _channel.QueueDeclareAsync(queue: topic + "." + Guid.NewGuid().ToString("N"),
            durable: false, exclusive: true, autoDelete: true, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(queue: queueName, exchange: _exchange, routingKey: topic, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var envelope = Deserialize(ea.Body.ToArray());
            if (envelope != null)
            {
                await handler(envelope);
            }
        };
        await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer, cancellationToken: cancellationToken);
    }

    public async Task UnsubscribeAsync(string topic)
    {
        // Auto-delete queues handle cleanup
        await Task.CompletedTask;
    }

    private static byte[] Serialize(EventEnvelope envelope)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

    private static EventEnvelope? Deserialize(byte[] data)
        => JsonSerializer.Deserialize<EventEnvelope>(Encoding.UTF8.GetString(data));

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        _channel.Dispose();
        _connection.Dispose();
    }
}
