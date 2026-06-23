using System.Text.Json;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Scheduling;
using Perp.Nexus.Core.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.Scheduling;

internal sealed class SchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageTransport _transport;
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(
        IServiceScopeFactory scopeFactory,
        IMessageTransport transport,
        ILogger<SchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _transport = transport;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message scheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<ISchedulerStore>();
                var dueMessages = await store.GetDueMessagesAsync(stoppingToken);

                foreach (var msg in dueMessages)
                {
                    try
                    {
                        var envelope = JsonSerializer.Deserialize<EventEnvelope>(msg.Payload);
                        if (envelope != null)
                        {
                            await _transport.PublishAsync(envelope, stoppingToken);
                            _logger.LogInformation("Executed scheduled message {Id} type {Type}", msg.Id, msg.Type);
                        }

                        await store.DeleteAsync(msg.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to execute scheduled message {Id}", msg.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduler loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
