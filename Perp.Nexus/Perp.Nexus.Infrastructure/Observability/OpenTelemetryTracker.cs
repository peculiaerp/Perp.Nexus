using System.Diagnostics;
using Perp.Nexus.Core.Messages;
using Perp.Nexus.Core.Observability;

namespace Perp.Nexus.Infrastructure.Observability;

internal sealed class OpenTelemetryTracker : IMessageTracker
{
    private static readonly ActivitySource ActivitySource = new("PerpNexus");

    public void TrackPublish(EventEnvelope envelope)
    {
        using var activity = ActivitySource.StartActivity("Message.Publish", ActivityKind.Producer);
        EnrichActivity(activity, envelope);
    }

    public void TrackConsume(EventEnvelope envelope, string consumerType, long elapsedMs)
    {
        using var activity = ActivitySource.StartActivity("Message.Consume", ActivityKind.Consumer);
        EnrichActivity(activity, envelope);
        activity?.SetTag("messaging.consumer_type", consumerType);
        activity?.SetTag("messaging.duration_ms", elapsedMs);
    }

    public void TrackSagaEvent(EventEnvelope envelope, string sagaType, string state)
    {
        using var activity = ActivitySource.StartActivity("Saga.Transition", ActivityKind.Internal);
        EnrichActivity(activity, envelope);
        activity?.SetTag("messaging.saga_type", sagaType);
        activity?.SetTag("messaging.saga_state", state);
    }

    public void TrackDeadLetter(EventEnvelope envelope, string reason)
    {
        using var activity = ActivitySource.StartActivity("Message.DeadLetter", ActivityKind.Internal);
        EnrichActivity(activity, envelope);
        activity?.SetTag("messaging.dead_letter_reason", reason);
        activity?.SetStatus(ActivityStatusCode.Error);
    }

    public void TrackRetry(EventEnvelope envelope, int attempt, string consumerType)
    {
        using var activity = ActivitySource.StartActivity("Message.Retry", ActivityKind.Internal);
        EnrichActivity(activity, envelope);
        activity?.SetTag("messaging.retry_attempt", attempt);
        activity?.SetTag("messaging.consumer_type", consumerType);
    }

    private static void EnrichActivity(Activity? activity, EventEnvelope envelope)
    {
        if (activity == null) return;
        activity.SetTag("messaging.message_id", envelope.MessageId.ToString());
        activity.SetTag("messaging.message_type", envelope.Type);
        activity.SetTag("messaging.correlation_id", envelope.CorrelationId.ToString());
        activity.SetTag("messaging.version", envelope.Version);
    }
}
