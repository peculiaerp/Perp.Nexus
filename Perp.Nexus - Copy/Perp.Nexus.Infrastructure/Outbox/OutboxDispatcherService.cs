using System.Text.Json;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Outbox;
using Perp.Nexus.Core.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.Outbox;

internal sealed class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageTransport _transport;
    private readonly ILogger<OutboxDispatcherService> _logger;

    public OutboxDispatcherService(
        IServiceScopeFactory scopeFactory,
        IMessageTransport transport,
        ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _transport = transport;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var messages = await store.GetUnprocessedAsync(50, stoppingToken);

                foreach (var msg in messages)
                {
                    try
                    {
                        var envelope = JsonSerializer.Deserialize<EventEnvelope>(msg.Payload);
                        if (envelope != null)
                        {
                            if (!string.IsNullOrEmpty(msg.Destination))
                                await _transport.SendAsync(envelope, stoppingToken);
                            else
                                await _transport.PublishAsync(envelope, stoppingToken);
                        }

                        await store.MarkAsProcessedAsync(msg.Id, stoppingToken);
                        _logger.LogDebug("Dispatched outbox message {Id}", msg.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to dispatch outbox message {Id}", msg.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox dispatcher loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
