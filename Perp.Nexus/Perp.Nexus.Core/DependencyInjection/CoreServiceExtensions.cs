using Microsoft.Extensions.DependencyInjection;
using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.DeadLetter;
using Perp.Nexus.Core.Inbox;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Observability;
using Perp.Nexus.Core.Outbox;
using Perp.Nexus.Core.Sagas;
using Perp.Nexus.Core.Scheduling;
using Perp.Nexus.Core.SchemaRegistry;
using Perp.Nexus.Core.Transports;

namespace Perp.Nexus.Core.DependencyInjection;

public sealed class NexusBuilder
{
    public IServiceCollection Services { get; }
    public NexusBuilder(IServiceCollection services) => Services = services;

    public NexusBuilder WithSagaPersistence<TStore>() where TStore : class, ISagaStore
    {
        Services.AddScoped<ISagaStore, TStore>();
        return this;
    }

    public NexusBuilder WithOutboxPersistence<TStore>() where TStore : class, IOutboxStore
    {
        Services.AddScoped<IOutboxStore, TStore>();
        return this;
    }

    public NexusBuilder WithInboxPersistence<TStore>() where TStore : class, IInboxStore
    {
        Services.AddScoped<IInboxStore, TStore>();
        return this;
    }

    public NexusBuilder WithDeadLetterPersistence<TStore>() where TStore : class, IDeadLetterStore
    {
        Services.AddScoped<IDeadLetterStore, TStore>();
        return this;
    }

    public NexusBuilder WithSchedulerPersistence<TStore>() where TStore : class, ISchedulerStore
    {
        Services.AddScoped<ISchedulerStore, TStore>();
        return this;
    }

    public NexusBuilder WithSchemaRegistry<TStore>() where TStore : class, ISchemaRegistry
    {
        Services.AddScoped<ISchemaRegistry, TStore>();
        return this;
    }

    public NexusBuilder WithTransport<TTransport>() where TTransport : class, IMessageTransport
    {
        Services.AddSingleton<IMessageTransport, TTransport>();
        return this;
    }
}

public static class CoreServiceExtensions
{
    public static NexusBuilder AddNexusCore(this IServiceCollection services)
    {
        services.AddScoped<IMessageTracker, DefaultMessageTracker>();
        return new NexusBuilder(services);
    }

    public static IServiceCollection AddMessageConsumer<TMessage, TConsumer>(this IServiceCollection services)
        where TMessage : class
        where TConsumer : class, IConsumer<TMessage>
    {
        services.AddScoped<IConsumer<TMessage>, TConsumer>();
        services.AddScoped<TConsumer>();
        services.AddKeyedScoped(typeof(IConsumer<TMessage>), typeof(TConsumer).FullName, (sp, _) => sp.GetRequiredService<IConsumer<TMessage>>());
        return services;
    }
}

internal sealed class DefaultMessageTracker : IMessageTracker
{
    public void TrackPublish(EventEnvelope envelope) { }
    public void TrackConsume(EventEnvelope envelope, string consumerType, long elapsedMs) { }
    public void TrackSagaEvent(EventEnvelope envelope, string sagaType, string state) { }
    public void TrackDeadLetter(EventEnvelope envelope, string reason) { }
    public void TrackRetry(EventEnvelope envelope, int attempt, string consumerType) { }
}
