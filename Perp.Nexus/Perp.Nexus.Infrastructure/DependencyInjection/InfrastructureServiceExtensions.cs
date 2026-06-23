using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.DeadLetter;
using Perp.Nexus.Core.DependencyInjection;
using Perp.Nexus.Core.Inbox;
using Perp.Nexus.Core.Middleware;
using Perp.Nexus.Core.Observability;
using Perp.Nexus.Core.Outbox;
using Perp.Nexus.Core.Sagas;
using Perp.Nexus.Core.Scheduling;
using Perp.Nexus.Core.SchemaRegistry;
using Perp.Nexus.Core.Transports;
using Perp.Nexus.Infrastructure.Middleware;
using Perp.Nexus.Infrastructure.Observability;
using Perp.Nexus.Infrastructure.Outbox;
using Perp.Nexus.Infrastructure.Persistence.EFCore;
using Perp.Nexus.Infrastructure.Sagas;
using Perp.Nexus.Infrastructure.Scheduling;
using Perp.Nexus.Infrastructure.SchemaRegistry;
using Perp.Nexus.Infrastructure.Transports;
using Perp.Nexus.Infrastructure.Transports.Kafka;
using Perp.Nexus.Infrastructure.Transports.RabbitMQ;
using StackExchange.Redis;

namespace Perp.Nexus.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static NexusBuilder AddNexusInfrastructure(this NexusBuilder builder, Action<InfrastructureOptions> configure)
    {
        var options = new InfrastructureOptions();
        configure(options);

        // Register EFCore DbContext
        builder.Services.AddDbContext<MessagingDbContext>(db =>
        {
            if (options.ConfigureDbContext != null)
            {
                options.ConfigureDbContext(db);
            }
            else
            {
                db.UseSqlServer(options.ConnectionString);
            }
        }, ServiceLifetime.Scoped);

        // Register stores
        builder.Services.AddScoped<IOutboxStore, OutboxStore>();
        builder.Services.AddScoped<IInboxStore, InboxStore>();
        builder.Services.AddScoped<IDeadLetterStore, DeadLetterStore>();
        builder.Services.AddScoped<ISchedulerStore, SchedulerStore>();
        builder.Services.AddScoped<ISchemaRegistry, SchemaRegistryStore>();

        // Register saga store (EF Core + optional Redis)
        builder.Services.AddScoped<ISagaStore, SagaStore>();

        if (!string.IsNullOrEmpty(options.RedisConnectionString))
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(options.RedisConnectionString));
            builder.Services.AddScoped<ISagaStore>(sp =>
                new CachedSagaStore(
                    new SagaStore(sp.GetRequiredService<MessagingDbContext>()),
                    sp.GetRequiredService<IConnectionMultiplexer>()));
        }

        // Register transport
        switch (options.TransportType?.ToLowerInvariant())
        {
            case "rabbitmq":
                builder.Services.AddSingleton<IMessageTransport>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<RabbitMqTransport>>();
                    return new RabbitMqTransport(options.TransportConnectionString!, options.RabbitMqExchange, logger);
                });
                break;
            case "kafka":
                builder.Services.AddSingleton<IMessageTransport>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<KafkaTransport>>();
                    return new KafkaTransport(options.TransportConnectionString!, logger);
                });
                break;
            default:
                builder.Services.AddSingleton<IMessageTransport, InMemoryTransport>();
                break;
        }

        // Register bus
        builder.Services.AddScoped<IBus, MessageBus>();

        // Register middleware pipeline
        builder.Services.AddScoped<MiddlewarePipeline>(sp =>
        {
            var middlewares = new List<IMiddleware>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            if (options.UseRetry)
                middlewares.Add(new RetryMiddleware(options.RetryMaxAttempts, options.RetryBaseDelay, loggerFactory.CreateLogger<RetryMiddleware>()));

            if (options.UseCircuitBreaker)
                middlewares.Add(new CircuitBreakerMiddleware(options.CircuitBreakerThreshold, options.CircuitBreakerResetTimeout, loggerFactory.CreateLogger<CircuitBreakerMiddleware>()));

            if (options.UseRateLimiting)
                middlewares.Add(new RateLimitingMiddleware(options.RateLimitMaxTokens, options.RateLimitReplenishmentPeriod));

            if (options.UseTracing)
                middlewares.Add(new OpenTelemetryTracingMiddleware());

            if (options.UseInboxDeduplication)
                middlewares.Add(new InboxDeduplicationMiddleware(sp.GetRequiredService<IInboxStore>(), loggerFactory.CreateLogger<InboxDeduplicationMiddleware>()));

            if (options.UseDeadLetter)
                middlewares.Add(new DeadLetterMiddleware(sp.GetRequiredService<IDeadLetterStore>(), loggerFactory.CreateLogger<DeadLetterMiddleware>()));

            return new MiddlewarePipeline(middlewares);
        });

        // Register background services
        if (options.UseOutbox)
            builder.Services.AddHostedService<OutboxDispatcherService>();

        if (options.UseScheduler)
            builder.Services.AddHostedService<SchedulerService>();

        builder.Services.AddHostedService<ConsumerDispatcherService>();

        // Register OpenTelemetry tracker
        builder.Services.AddScoped<IMessageTracker, OpenTelemetryTracker>();

        // Register schema for known types
        if (options.AutoRegisterSchemas)
        {
            builder.Services.AddHostedService<SchemaAutoRegisterService>();
        }

        return builder;
    }

    public static NexusBuilder AddRabbitMqTransport(this NexusBuilder builder, string connectionString, string exchange = "masstransist")
    {
        builder.Services.AddSingleton<IMessageTransport>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqTransport>>();
            return new RabbitMqTransport(connectionString, exchange, logger);
        });
        return builder;
    }

    public static NexusBuilder AddKafkaTransport(this NexusBuilder builder, string bootstrapServers)
    {
        builder.Services.AddSingleton<IMessageTransport>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<KafkaTransport>>();
            return new KafkaTransport(bootstrapServers, logger);
        });
        return builder;
    }

    public static NexusBuilder AddInMemoryTransport(this NexusBuilder builder)
    {
        builder.Services.AddSingleton<IMessageTransport, InMemoryTransport>();
        return builder;
    }
}

public sealed class InfrastructureOptions
{
    public string? ConnectionString { get; set; }
    public string? RedisConnectionString { get; set; }
    public string? TransportType { get; set; }
    public string? TransportConnectionString { get; set; }
    public string RabbitMqExchange { get; set; } = "masstransist";
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    public bool UseRetry { get; set; } = true;
    public int RetryMaxAttempts { get; set; } = 3;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public bool UseCircuitBreaker { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerResetTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool UseRateLimiting { get; set; }
    public int RateLimitMaxTokens { get; set; } = 100;
    public TimeSpan RateLimitReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(1);

    public bool UseTracing { get; set; } = true;
    public bool UseInboxDeduplication { get; set; } = true;
    public bool UseDeadLetter { get; set; } = true;
    public bool UseOutbox { get; set; } = true;
    public bool UseScheduler { get; set; } = true;
    public bool AutoRegisterSchemas { get; set; } = true;
}
