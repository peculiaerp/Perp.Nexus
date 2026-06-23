using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.SchemaRegistry;

internal sealed class SchemaAutoRegisterService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SchemaAutoRegisterService> _logger;

    public SchemaAutoRegisterService(IServiceProvider serviceProvider, ILogger<SchemaAutoRegisterService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ISchemaRegistry>();

        var consumerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)))
            .ToList();

        var registered = new HashSet<string>();

        foreach (var consumerType in consumerTypes)
        {
            var messageTypes = consumerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => i.GetGenericArguments()[0]);

            foreach (var messageType in messageTypes)
            {
                var typeName = messageType.FullName!;
                if (registered.Add(typeName))
                {
                    try
                    {
                        var registerMethod = typeof(ISchemaRegistry)
                            .GetMethod(nameof(ISchemaRegistry.RegisterAsync))!
                            .MakeGenericMethod(messageType);

                        await (Task)registerMethod.Invoke(registry, [1, stoppingToken])!;
                        _logger.LogInformation("Registered schema for {Type}", typeName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to register schema for {Type}", typeName);
                    }
                }
            }
        }
    }
}
