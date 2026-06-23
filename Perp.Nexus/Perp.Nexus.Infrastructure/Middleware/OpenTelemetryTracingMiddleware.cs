using System.Diagnostics;
using Perp.Nexus.Core.Middleware;

namespace Perp.Nexus.Infrastructure.Middleware;

internal sealed class OpenTelemetryTracingMiddleware : IMiddleware
{
    private static readonly ActivitySource ActivitySource = new("Perp.Nexus.Middleware");

    public async Task InvokeAsync(MiddlewareContext context, Func<Task> next)
    {
        using var activity = ActivitySource.StartActivity("Middleware." + context.Action, ActivityKind.Internal);
        if (activity != null)
        {
            activity.SetTag("messaging.message_id", context.Envelope.MessageId.ToString());
            activity.SetTag("messaging.message_type", context.Envelope.Type);
            activity.SetTag("messaging.correlation_id", context.Envelope.CorrelationId.ToString());
            activity.SetTag("messaging.action", context.Action);

            foreach (var header in context.Envelope.Headers)
            {
                activity.SetTag("messaging.header." + header.Key, header.Value);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await next();
        }
        finally
        {
            sw.Stop();
            activity?.SetTag("messaging.duration_ms", sw.ElapsedMilliseconds);
        }
    }
}
