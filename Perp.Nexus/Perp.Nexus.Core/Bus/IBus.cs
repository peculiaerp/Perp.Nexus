using Perp.Nexus.Core.Messages;

namespace Perp.Nexus.Core.Bus;

public interface IBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task SendAsync<T>(T message, string destination, CancellationToken cancellationToken = default) where T : class;
    Task PublishWithCorrelationAsync<T>(T message, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default) where T : class;
    Task SendWithCorrelationAsync<T>(T message, string destination, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default) where T : class;
}
