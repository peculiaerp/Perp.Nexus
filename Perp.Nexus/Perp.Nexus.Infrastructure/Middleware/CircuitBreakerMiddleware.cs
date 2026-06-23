using Microsoft.Extensions.Logging;
using Perp.Nexus.Core.Middleware;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class CircuitBreakerMiddleware : IMiddleware
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private readonly ILogger<CircuitBreakerMiddleware> _logger;
    private readonly object _lock = new();
    private int _failureCount;
    private DateTime _lastFailure;
    private CircuitState _state = CircuitState.Closed;

    public CircuitBreakerMiddleware(int failureThreshold = 5, TimeSpan? resetTimeout = null, ILogger<CircuitBreakerMiddleware>? logger = null)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout ?? TimeSpan.FromSeconds(30);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitBreakerMiddleware>.Instance;
    }

    public async Task InvokeAsync(MiddlewareContext context, Func<Task> next)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailure > _resetTimeout)
            {
                lock (_lock)
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation("Circuit breaker half-open for message {MessageId}", context.Envelope.MessageId);
                }
            }
            else
            {
                _logger.LogWarning("Circuit breaker open, rejecting message {MessageId}", context.Envelope.MessageId);
                throw new CircuitBreakerOpenException($"Circuit breaker is open for {context.Envelope.Type}");
            }
        }

        try
        {
            await next();
            if (_state == CircuitState.HalfOpen)
            {
                lock (_lock)
                {
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                    _logger.LogInformation("Circuit breaker reset to closed");
                }
            }
        }
        catch (Exception)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailure = DateTime.UtcNow;
                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _logger.LogError("Circuit breaker opened after {Count} failures", _failureCount);
                }
            }
            throw;
        }
    }

    private enum CircuitState { Closed, Open, HalfOpen }
}

public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
