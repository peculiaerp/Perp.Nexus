using Microsoft.AspNetCore.SignalR;
using Perp.Nexus.Core.DeadLetter;
using Perp.Nexus.Core.Outbox;
using Perp.Nexus.Core.Scheduling;

namespace Perp.Nexus.AdminDashboard.Hubs;

internal sealed class MessagingHub : Hub
{
    private readonly IDeadLetterStore _deadLetterStore;
    private readonly IOutboxStore _outboxStore;
    private readonly ISchedulerStore _schedulerStore;

    public MessagingHub(
        IDeadLetterStore deadLetterStore,
        IOutboxStore outboxStore,
        ISchedulerStore schedulerStore)
    {
        _deadLetterStore = deadLetterStore;
        _outboxStore = outboxStore;
        _schedulerStore = schedulerStore;
    }

    public async Task GetDashboardData()
    {
        var deadLetterCount = await _deadLetterStore.GetCountAsync();
        var pendingOutboxCount = await _outboxStore.GetPendingCountAsync();
        var pendingScheduledCount = await _schedulerStore.GetPendingCountAsync();

        await Clients.Caller.SendAsync("DashboardUpdate", new
        {
            DeadLetterCount = deadLetterCount,
            PendingOutboxCount = pendingOutboxCount,
            PendingScheduledCount = pendingScheduledCount,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task GetDeadLetterMessages()
    {
        var messages = await _deadLetterStore.GetAllAsync();
        await Clients.Caller.SendAsync("DeadLetterMessages", messages);
    }

    public static async Task NotifyMessagePublished(IHubContext<MessagingHub> hubContext, string type, Guid messageId, Guid correlationId)
    {
        await hubContext.Clients.All.SendAsync("MessagePublished", new
        {
            Type = type,
            MessageId = messageId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        });
    }

    public static async Task NotifySagaTransition(IHubContext<MessagingHub> hubContext, Guid correlationId, string sagaType, string state, string? lastEvent)
    {
        await hubContext.Clients.All.SendAsync("SagaTransition", new
        {
            CorrelationId = correlationId,
            SagaType = sagaType,
            State = state,
            LastEvent = lastEvent,
            Timestamp = DateTime.UtcNow
        });
    }

    public static async Task NotifyDeadLetterEvent(IHubContext<MessagingHub> hubContext, string messageType, string errorReason)
    {
        await hubContext.Clients.All.SendAsync("DeadLetterEvent", new
        {
            MessageType = messageType,
            ErrorReason = errorReason,
            Timestamp = DateTime.UtcNow
        });
    }

    public static async Task NotifyRetryAttempt(IHubContext<MessagingHub> hubContext, Guid messageId, int attempt, string consumerType)
    {
        await hubContext.Clients.All.SendAsync("RetryAttempt", new
        {
            MessageId = messageId,
            Attempt = attempt,
            ConsumerType = consumerType,
            Timestamp = DateTime.UtcNow
        });
    }
}
