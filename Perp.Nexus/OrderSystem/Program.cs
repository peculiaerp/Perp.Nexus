using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSystem.Consumers;
using OrderSystem.Events;
using OrderSystem.Sagas;
using Perp.Nexus.Core.Bus;
using Perp.Nexus.Core.Sagas;
using Perp.Nexus.Infrastructure.DependencyInjection;
using Perp.Nexus.Infrastructure.Persistence.EFCore;

var builder = Host.CreateApplicationBuilder(args);

// Register EF Core with the desired provider
builder.Services.AddDbContext<MessagingDbContext>(db =>
    db.UseSqlite("Data Source=perpnexus.db"), ServiceLifetime.Scoped);

// Fluent MassTransit-style configuration
builder.Services.AddPerpNexus(config =>
{
    config.AddConsumer<OrderCreated, OrderCreatedConsumer>();
    config.AddConsumer<PaymentReceived, PaymentReceivedConsumer>();
    config.AddConsumer<InventoryReserved, InventoryReservedConsumer>();
    config.AddConsumer<OrderShipped, OrderShippedConsumer>();

    config.AddSaga<OrderSagaOrchestrator, OrderSagaState>(sp =>
        new OrderSagaOrchestrator(
            sp.GetRequiredService<ISagaStore>(),
            sp.GetRequiredService<IBus>()));

    config.UsingInMemory();
    config.WithEntityFrameworkPersistence();
    config.EnableRetry(maxAttempts: 3);
    config.EnableTracing();
    config.EnableInbox();
    config.EnableDeadLetter();
});

using var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var bus = app.Services.GetRequiredService<IBus>();

logger.LogInformation("=== Order Saga Demo Starting ===");

var cts = new CancellationTokenSource();
await app.StartAsync(cts.Token);

// Ensure database schema is created
using (var initScope = app.Services.CreateScope())
{
    var db = initScope.ServiceProvider.GetRequiredService<MessagingDbContext>();

#pragma warning disable CA1849 // Call async methods when in an async method
    db.Database.EnsureCreated();
#pragma warning restore CA1849 // Call async methods when in an async method
}

await Task.Delay(500);

var orderId = Guid.NewGuid();
logger.LogInformation("Creating order {OrderId}", orderId);

await bus.PublishWithCorrelationAsync(
    new OrderCreated(
        orderId,
        "CUST-001",
        299.99m,
        new List<OrderItem>
        {
            new("PROD-001", "Wireless Mouse", 2, 49.99m),
            new("PROD-002", "Mechanical Keyboard", 1, 199.99m)
        }),
    orderId);

await Task.Delay(3000);

using var scope = app.Services.CreateScope();
var store = scope.ServiceProvider.GetRequiredService<ISagaStore>();
var sagaState = await store.LoadAsync<OrderSagaState>(orderId);

if (sagaState != null)
{
    logger.LogInformation("=== Saga Result ===");
    logger.LogInformation("State: {State}", sagaState.CurrentState);
    logger.LogInformation("CompletedAt: {At}", sagaState.CompletedAt?.ToString("O") ?? "N/A");
    logger.LogInformation("Transaction: {Tx}", sagaState.TransactionId ?? "N/A");
    logger.LogInformation("Tracking: {Tracking}", sagaState.TrackingNumber ?? "N/A");
    logger.LogInformation("Carrier: {Carrier}", sagaState.Carrier ?? "N/A");
}
else
{
    logger.LogWarning("No saga state found for {OrderId}", orderId);
}

await cts.CancelAsync();
await app.StopAsync();

if (sagaState?.CurrentState == "Completed")
{
    logger.LogInformation("=== SUCCESS: Order saga completed successfully ===");
}
else
{
    logger.LogWarning("=== Order saga did not complete as expected ===");
}
