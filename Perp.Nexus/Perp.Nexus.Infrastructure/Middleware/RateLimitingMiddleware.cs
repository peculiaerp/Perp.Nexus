using System.Threading.RateLimiting;
using Perp.Nexus.Core.Middleware;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class RateLimitingMiddleware : IMiddleware, IAsyncDisposable
{
    private readonly TokenBucketRateLimiter _rateLimiter;

    public RateLimitingMiddleware(int maxTokens = 100, TimeSpan? replenishmentPeriod = null)
    {
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = maxTokens,
            QueueLimit = 0,
            ReplenishmentPeriod = replenishmentPeriod ?? TimeSpan.FromSeconds(1),
            TokensPerPeriod = maxTokens / 2,
            AutoReplenishment = true
        });
    }

    public async Task InvokeAsync(MiddlewareContext context, Func<Task> next)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, context.CancellationToken);
        if (!lease.IsAcquired)
        {
            throw new RateLimitExceededException($"Rate limit exceeded for {context.Envelope.Type}");
        }
        await next();
    }

    public async ValueTask DisposeAsync()
    {
        await _rateLimiter.DisposeAsync();
    }
}

public sealed class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}
