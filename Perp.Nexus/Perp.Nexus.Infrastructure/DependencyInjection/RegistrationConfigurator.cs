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

public static class PerpNexusRegistrationExtensions
{
    public static IServiceCollection AddPerpNexus(this IServiceCollection services, Action<IRegistrationConfigurator> configure)
    {
        var configurator = new RegistrationConfigurator(services);
        configure(configurator);
        configurator.Build();
        return services;
    }
}

internal sealed class RegistrationConfigurator : IRegistrationConfigurator
{
    private readonly IServiceCollection _services;
    private string? _redisConnectionString;
    private string? _transportConnectionString;
    private string _rabbitMqExchange = "perpnexus";
    private TransportType? _transportType;
    ////private readonly List<Action> _buildActions = new();
    private readonly PipelineConfigurator _pipeline = new();

    public RegistrationConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public IRegistrationConfigurator AddConsumer<TMessage, TConsumer>()
        where TConsumer : class, IConsumer<TMessage>
    {
        _services.AddScoped<IConsumer<TMessage>, TConsumer>();
        _services.AddScoped<TConsumer>();
        return this;
    }

    public IRegistrationConfigurator AddSaga<TSaga, TState>()
        where TSaga : class, ISagaOrchestrator
        where TState : SagaState
    {
        _services.AddScoped<ISagaOrchestrator, TSaga>();
        return this;
    }

    public IRegistrationConfigurator AddSaga<TSaga, TState>(Func<IServiceProvider, TSaga> factory)
        where TSaga : class, ISagaOrchestrator
        where TState : SagaState
    {
        _services.AddScoped<ISagaOrchestrator>(factory);
        return this;
    }

    public IRegistrationConfigurator AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware
    {
        _services.AddScoped<IMiddleware, TMiddleware>();
        return this;
    }

    public IRegistrationConfigurator UsingInMemory()
    {
        _transportType = TransportType.InMemory;
        return this;
    }

    public IRegistrationConfigurator UsingRabbitMq(string connectionString, string exchange = "perpnexus")
    {
        _transportType = TransportType.RabbitMQ;
        _transportConnectionString = connectionString;
        _rabbitMqExchange = exchange;
        return this;
    }

    public IRegistrationConfigurator UsingKafka(string bootstrapServers)
    {
        _transportType = TransportType.Kafka;
        _transportConnectionString = bootstrapServers;
        return this;
    }

    public IRegistrationConfigurator WithTransport<TTransport>()
        where TTransport : class, IMessageTransport
    {
        _services.AddSingleton<IMessageTransport, TTransport>();
        return this;
    }

    public IRegistrationConfigurator WithEntityFrameworkPersistence()
    {
        _services.AddScoped<IOutboxStore, OutboxStore>();
        _services.AddScoped<IInboxStore, InboxStore>();
        _services.AddScoped<IDeadLetterStore, DeadLetterStore>();
        _services.AddScoped<ISchedulerStore, SchedulerStore>();
        _services.AddScoped<ISchemaRegistry, SchemaRegistryStore>();
        _services.AddScoped<ISagaStore, SagaStore>();

        return this;
    }

    public IRegistrationConfigurator WithRedisCache(string connectionString)
    {
        _redisConnectionString = connectionString;
        _services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(connectionString));
        _services.AddScoped<ISagaStore>(sp =>
            new CachedSagaStore(
                new SagaStore(sp.GetRequiredService<MessagingDbContext>()),
                sp.GetRequiredService<IConnectionMultiplexer>()));
        return this;
    }

    public IRegistrationConfigurator ConfigurePipeline(Action<IPipelineConfigurator> configure)
    {
        configure(_pipeline);
        return this;
    }

    public IRegistrationConfigurator EnableOutbox()
    {
        _services.AddHostedService<OutboxDispatcherService>();
        return this;
    }

    public IRegistrationConfigurator EnableInbox()
    {
        _pipeline.AddInboxDeduplication();
        return this;
    }

    public IRegistrationConfigurator EnableScheduler()
    {
        _services.AddHostedService<SchedulerService>();
        return this;
    }

    public IRegistrationConfigurator EnableDeadLetter()
    {
        _pipeline.AddDeadLetter();
        return this;
    }

    public IRegistrationConfigurator EnableRetry(int maxAttempts = 3, TimeSpan? baseDelay = null)
    {
        _pipeline.AddRetry(maxAttempts, baseDelay);
        return this;
    }

    public IRegistrationConfigurator EnableCircuitBreaker(int threshold = 5, TimeSpan? resetTimeout = null)
    {
        _pipeline.AddCircuitBreaker(threshold, resetTimeout);
        return this;
    }

    public IRegistrationConfigurator EnableRateLimiting(int maxTokens = 100, TimeSpan? replenishmentPeriod = null)
    {
        _pipeline.AddRateLimiting(maxTokens, replenishmentPeriod);
        return this;
    }

    public IRegistrationConfigurator EnableTracing()
    {
        _pipeline.AddTracing();
        return this;
    }

    public IRegistrationConfigurator UseSchemaRegistry(bool autoRegister = true)
    {
        if (autoRegister)
            _services.AddHostedService<SchemaAutoRegisterService>();
        return this;
    }

    public void Build()
    {
        RegisterTransport();

        _services.AddScoped<IBus, MessageBus>();

        _services.AddScoped<MiddlewarePipeline>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var middlewares = _pipeline.Build(sp, loggerFactory);
            return new MiddlewarePipeline(middlewares);
        });

        _services.AddHostedService<ConsumerDispatcherService>();

        _services.AddScoped<IMessageTracker, OpenTelemetryTracker>();
    }

    private void RegisterTransport()
    {
        switch (_transportType)
        {
            case TransportType.RabbitMQ:
                _services.AddSingleton<IMessageTransport>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<RabbitMqTransport>>();
                    return new RabbitMqTransport(_transportConnectionString!, _rabbitMqExchange, logger);
                });
                break;
            case TransportType.Kafka:
                _services.AddSingleton<IMessageTransport>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<KafkaTransport>>();
                    return new KafkaTransport(_transportConnectionString!, logger);
                });
                break;
            default:
                _services.AddSingleton<IMessageTransport, InMemoryTransport>();
                break;
        }
    }
}

internal sealed class PipelineConfigurator : IPipelineConfigurator
{
    private readonly List<Func<IServiceProvider, ILoggerFactory, IMiddleware>> _middlewareFactories = new();

    public IPipelineConfigurator AddRetry(int maxAttempts = 3, TimeSpan? baseDelay = null)
    {
        _middlewareFactories.Add((sp, lf) =>
            new RetryMiddleware(maxAttempts, baseDelay, lf.CreateLogger<RetryMiddleware>()));
        return this;
    }

    public IPipelineConfigurator AddCircuitBreaker(int threshold = 5, TimeSpan? resetTimeout = null)
    {
        _middlewareFactories.Add((sp, lf) =>
            new CircuitBreakerMiddleware(threshold, resetTimeout, lf.CreateLogger<CircuitBreakerMiddleware>()));
        return this;
    }

    public IPipelineConfigurator AddRateLimiting(int maxTokens = 100, TimeSpan? replenishmentPeriod = null)
    {
        _middlewareFactories.Add((_, _) => new RateLimitingMiddleware(maxTokens, replenishmentPeriod));
        return this;
    }

    public IPipelineConfigurator AddTracing()
    {
        _middlewareFactories.Add((_, _) => new OpenTelemetryTracingMiddleware());
        return this;
    }

    public IPipelineConfigurator AddInboxDeduplication()
    {
        _middlewareFactories.Add((sp, lf) =>
            new InboxDeduplicationMiddleware(
                sp.GetRequiredService<IInboxStore>(), lf.CreateLogger<InboxDeduplicationMiddleware>()));
        return this;
    }

    public IPipelineConfigurator AddDeadLetter()
    {
        _middlewareFactories.Add((sp, lf) =>
            new DeadLetterMiddleware(
                sp.GetRequiredService<IDeadLetterStore>(), lf.CreateLogger<DeadLetterMiddleware>()));
        return this;
    }

    public List<IMiddleware> Build(IServiceProvider sp, ILoggerFactory lf)
        => _middlewareFactories.Select(f => f(sp, lf)).ToList();
}
