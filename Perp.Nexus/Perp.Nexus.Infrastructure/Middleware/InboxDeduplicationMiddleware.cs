using Perp.Nexus.Core.Inbox;
using Perp.Nexus.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class InboxDeduplicationMiddleware : IMiddleware
{
    private readonly IInboxStore _inboxStore;
    private readonly ILogger<InboxDeduplicationMiddleware> _logger;

    public InboxDeduplicationMiddleware(IInboxStore inboxStore, ILogger<InboxDeduplicationMiddleware>? logger = null)
    {
        _inboxStore = inboxStore;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InboxDeduplicationMiddleware>.Instance;
    }

    public async Task InvokeAsync(MiddlewareContext context, Func<Task> next)
    {
        var messageId = context.Envelope.MessageId;
        if (await _inboxStore.IsProcessedAsync(messageId, context.CancellationToken))
        {
            _logger.LogInformation("Message {MessageId} already processed, skipping", messageId);
            return;
        }

        await next();

        await _inboxStore.MarkAsProcessedAsync(messageId, context.Envelope.Type, context.CancellationToken);
    }
}
