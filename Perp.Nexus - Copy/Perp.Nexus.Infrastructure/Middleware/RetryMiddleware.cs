using Perp.Nexus.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class RetryMiddleware : IMiddleware
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly ILogger<RetryMiddleware> _logger;

    public RetryMiddleware(int maxRetries = 3, TimeSpan? baseDelay = null, ILogger<RetryMiddleware>? logger = null)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(200);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RetryMiddleware>.Instance;
    }

    public async Task InvokeAsync(MiddlewareContext context, Func<Task> next)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await next();
                return;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex, "Retry {Attempt}/{MaxRetries} for message {MessageId} after {Delay}ms",
                    attempt, _maxRetries, context.Envelope.MessageId, delay.TotalMilliseconds);
                context.Items["RetryAttempt"] = attempt;
                await Task.Delay(delay, context.CancellationToken);
            }
        }
    }
}
