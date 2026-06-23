using Perp.Nexus.Core.Messages;

namespace Perp.Nexus.Core.Middleware;

public interface IMiddleware
{
    Task InvokeAsync(MiddlewareContext context, Func<Task> next);
}

public sealed class MiddlewareContext
{
    public required EventEnvelope Envelope { get; init; }
    public required string Action { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
    public Dictionary<string, object> Items { get; init; } = new();
    public CancellationToken CancellationToken { get; init; }
}

public sealed class MiddlewarePipeline
{
    private readonly List<IMiddleware> _middlewares;

    public MiddlewarePipeline(IEnumerable<IMiddleware> middlewares)
    {
        _middlewares = middlewares.ToList();
    }

    public async Task ExecuteAsync(MiddlewareContext context, Func<Task>? finalAction = null)
    {
        var index = 0;

        async Task Next()
        {
            if (index < _middlewares.Count)
            {
                var middleware = _middlewares[index++];
                await middleware.InvokeAsync(context, Next);
            }
            else if (finalAction != null)
            {
                await finalAction();
            }
        }

        await Next();
    }
}
