using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Transports;

namespace Perp.Nexus.Infrastructure.Transports.Kafka;

internal sealed class KafkaTransport : IMessageTransport, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaTransport> _logger;
    private IConsumer<string, string>? _consumer;
    private CancellationTokenSource? _subscriptionCts;

    public KafkaTransport(string bootstrapServers, ILogger<KafkaTransport>? logger = null)
    {
        _bootstrapServers = bootstrapServers;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<KafkaTransport>.Instance;

        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public string Name => "Kafka";

    public async Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var key = envelope.Headers.GetValueOrDefault("partitionKey", envelope.CorrelationId.ToString());
        var result = await _producer.ProduceAsync(envelope.Type,
            new Message<string, string> { Key = key, Value = JsonSerializer.Serialize(envelope) },
            cancellationToken);
        _logger.LogDebug("Sent {Type} to Kafka topic {Topic} partition {Partition}",
            envelope.Type, result.Topic, result.Partition.Value);
    }

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var key = envelope.Headers.GetValueOrDefault("partitionKey", envelope.CorrelationId.ToString());
        await _producer.ProduceAsync(envelope.Type,
            new Message<string, string> { Key = key, Value = JsonSerializer.Serialize(envelope) },
            cancellationToken);
        _logger.LogDebug("Published {Type} to Kafka topic {Topic}", envelope.Type, envelope.Type);
    }

    public Task SubscribeAsync(string topic, Func<EventEnvelope, Task> handler, CancellationToken cancellationToken = default)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "masstransist-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(topic);
        _subscriptionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!_subscriptionCts.Token.IsCancellationRequested)
                {
                    var result = _consumer.Consume(_subscriptionCts.Token);
                    if (result != null)
                    {
                        var envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Message.Value);
                        if (envelope != null)
                        {
                            await handler(envelope);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, _subscriptionCts.Token);

        return Task.CompletedTask;
    }

    public async Task UnsubscribeAsync(string topic)
    {
        await _subscriptionCts!.CancelAsync();
        _consumer?.Unsubscribe();
        ////return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscriptionCts?.Cancel();
        _consumer?.Close();
        _consumer?.Dispose();
        _producer?.Flush();
        _producer?.Dispose();
    }
}
