using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.Outbox;
using Perp.Nexus.Core.Inbox;
using Perp.Nexus.Core.DeadLetter;
using Perp.Nexus.Core.Scheduling;
using Perp.Nexus.Core.Sagas;
using Perp.Nexus.Core.SchemaRegistry;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

public sealed class MessagingDbContext : DbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();
    public DbSet<MessageSchema> MessageSchemas => Set<MessageSchema>();
    public DbSet<SagaStateEntity> SagaStates => Set<SagaStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(500);
            e.Property(x => x.Payload);
            e.HasIndex(x => x.IsProcessed);
            e.HasIndex(x => x.OccurredOn);
        });

        modelBuilder.Entity<InboxMessage>(e =>
        {
            e.HasKey(x => x.MessageId);
            e.Property(x => x.MessageType).HasMaxLength(500);
            e.HasIndex(x => x.ReceivedAt);
        });

        modelBuilder.Entity<DeadLetterMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MessageType).HasMaxLength(500);
            e.Property(x => x.Payload);
            e.Property(x => x.ErrorReason).HasMaxLength(4000);
            e.HasIndex(x => x.Timestamp);
        });

        modelBuilder.Entity<ScheduledMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(500);
            e.Property(x => x.Payload);
            e.HasIndex(x => x.ExecuteAt);
        });

        modelBuilder.Entity<MessageSchema>(e =>
        {
            e.HasKey(x => new { x.Type, x.Version });
            e.Property(x => x.SchemaJson);
            e.Property(x => x.Type).HasMaxLength(500);
        });

        modelBuilder.Entity<SagaStateEntity>(e =>
        {
            e.HasKey(x => x.CorrelationId);
            e.Property(x => x.SagaType).HasMaxLength(500);
            e.Property(x => x.CurrentState).HasMaxLength(200);
            e.Property(x => x.StateData);
            e.HasIndex(x => new { x.SagaType, x.CurrentState });
        });
    }
}
