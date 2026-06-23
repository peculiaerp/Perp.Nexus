using Perp.Nexus.Core.DeadLetter;
using Perp.Nexus.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class DeadLetterMiddleware : IMiddleware
{
    private readonly IDeadLetterStore _deadLetterStore;
    private readonly ILogger<DeadLetterMiddleware> _logger;

    public DeadLetterMiddleware(IDeadLetterStore deadLetterStore, ILogger<DeadLetterMiddleware>? logger = null)
    {
        _deadLetterStore = deadLetterStore;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DeadLetterMiddleware>.Instance;
    }

    public async Task InvokeAsync(MiddlewareContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message {MessageId} failed, sending to dead letter", context.Envelope.MessageId);

            var retryAttempt = context.Items.GetValueOrDefault("RetryAttempt") as int? ?? 0;

            await _deadLetterStore.AddAsync(new DeadLetterMessage
            {
                Id = Guid.NewGuid(),
                MessageType = context.Envelope.Type,
                Payload = context.Envelope.Payload,
                ErrorReason = ex.Message,
                StackTrace = ex.ToString(),
                CorrelationId = context.Envelope.CorrelationId,
                Timestamp = DateTime.UtcNow,
                RetryCount = retryAttempt,
                ExceptionType = ex.GetType().FullName
            }, context.CancellationToken);
        }
    }
}
