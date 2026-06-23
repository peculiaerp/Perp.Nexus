using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Middleware;
using Perp.Nexus.Core.Sagas;
using Perp.Nexus.Core.Transports;

namespace Perp.Nexus.Core.DependencyInjection;

public interface IRegistrationConfigurator
{
    IRegistrationConfigurator AddConsumer<TMessage, TConsumer>()
        where TConsumer : class, IConsumer<TMessage>;

    IRegistrationConfigurator AddSaga<TSaga, TState>()
        where TSaga : class, ISagaOrchestrator
        where TState : SagaState;

    IRegistrationConfigurator AddSaga<TSaga, TState>(Func<IServiceProvider, TSaga> factory)
        where TSaga : class, ISagaOrchestrator
        where TState : SagaState;

    IRegistrationConfigurator AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware;

    IRegistrationConfigurator UsingInMemory();

    IRegistrationConfigurator UsingRabbitMq(string connectionString, string exchange = "perpnexus");

    IRegistrationConfigurator UsingKafka(string bootstrapServers);

    IRegistrationConfigurator WithTransport<TTransport>()
        where TTransport : class, IMessageTransport;

    IRegistrationConfigurator WithEntityFrameworkPersistence();

    IRegistrationConfigurator WithRedisCache(string connectionString);

    IRegistrationConfigurator ConfigurePipeline(Action<IPipelineConfigurator> configure);

    IRegistrationConfigurator EnableOutbox();

    IRegistrationConfigurator EnableInbox();

    IRegistrationConfigurator EnableScheduler();

    IRegistrationConfigurator EnableDeadLetter();

    IRegistrationConfigurator EnableRetry(int maxAttempts = 3, TimeSpan? baseDelay = null);

    IRegistrationConfigurator EnableCircuitBreaker(int threshold = 5, TimeSpan? resetTimeout = null);

    IRegistrationConfigurator EnableRateLimiting(int maxTokens = 100, TimeSpan? replenishmentPeriod = null);

    IRegistrationConfigurator EnableTracing();

    IRegistrationConfigurator UseSchemaRegistry(bool autoRegister = true);
}
