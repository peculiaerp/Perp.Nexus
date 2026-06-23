using Perp.Nexus.Core.Messages;

namespace Perp.Nexus.Core.Bus;

public interface IConsumer<T>
{
    Task ConsumeAsync(ConsumeContext<T> context, CancellationToken cancellationToken = default);
}

public sealed class ConsumeContext<T>
{
    public required T Message { get; init; }
    public required EventEnvelope Envelope { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }

    public ConsumeContext<T> WithCancellation(CancellationToken ct)
        => new() { Message = Message, Envelope = Envelope, ServiceProvider = ServiceProvider, CancellationToken = ct };
}
