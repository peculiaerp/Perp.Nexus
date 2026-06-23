using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Middleware;
using Perp.Nexus.Core.Transports;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class ConsumerDispatcherService : BackgroundService, IAsyncDisposable
{
    private readonly IMessageTransport _transport;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerDispatcherService> _logger;

    public ConsumerDispatcherService(
        IMessageTransport transport,
        IServiceProvider serviceProvider,
        ILogger<ConsumerDispatcherService> logger)
    {
        _transport = transport;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer dispatcher started");

        var consumerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)))
            .ToList();

        var topics = new List<string>();

        foreach (var consumerType in consumerTypes)
        {
            var messageType = consumerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .GetGenericArguments()[0];

            var topic = messageType.FullName!;
            topics.Add(topic);

            await _transport.SubscribeAsync(topic, async envelope =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var middlewarePipeline = scope.ServiceProvider.GetRequiredService<MiddlewarePipeline>();

                    var middlewareContext = new MiddlewareContext
                    {
                        Envelope = envelope,
                        Action = "Consume",
                        ServiceProvider = scope.ServiceProvider,
                        CancellationToken = CancellationToken.None
                    };

                    await middlewarePipeline.ExecuteAsync(middlewareContext, async () =>
                    {
                        var consumer = scope.ServiceProvider.GetRequiredService(consumerType);
                        var message = JsonSerializer.Deserialize(envelope.Payload, messageType);
                        if (message != null)
                        {
                            var contextType = typeof(ConsumeContext<>).MakeGenericType(messageType);
                            var context = Activator.CreateInstance(contextType);
                            contextType.GetProperty("Message")!.SetValue(context, message);
                            contextType.GetProperty("Envelope")!.SetValue(context, envelope);
                            contextType.GetProperty("ServiceProvider")!.SetValue(context, scope.ServiceProvider);

                            var consumeMethod = consumerType.GetMethod("ConsumeAsync");
                            if (consumeMethod != null)
                            {
                                await (Task)consumeMethod.Invoke(consumer, [context, CancellationToken.None])!;
                            }
                        }
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Consumer failed for message {MessageId} type {Type}",
                        envelope.MessageId, envelope.Type);
                }
            }, stoppingToken);
        }

        _logger.LogInformation("Subscribed to {Count} topics: {Topics}", topics.Count, string.Join(", ", topics));

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Consumer dispatcher stopping");
        }
    }
}
