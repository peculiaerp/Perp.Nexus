using Microsoft.EntityFrameworkCore;
using Perp.Nexus.AdminDashboard.Hubs;
using Perp.Nexus.Infrastructure.DependencyInjection;
using Perp.Nexus.Infrastructure.Persistence.EFCore;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core with the configured provider
builder.Services.AddDbContext<MessagingDbContext>(db =>
    db.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=PerpNexus;Trusted_Connection=True;"),
    ServiceLifetime.Scoped);

// Fluent configuration
builder.Services.AddPerpNexus(config =>
{
    var transportType = builder.Configuration.GetValue<string>("Transport:Type") ?? "InMemory";
    if (transportType == "RabbitMQ")
        config.UsingRabbitMq(builder.Configuration.GetConnectionString("RabbitMQ") ?? "localhost");
    else if (transportType == "Kafka")
        config.UsingKafka(builder.Configuration.GetConnectionString("Kafka") ?? "localhost:9092");
    else
        config.UsingInMemory();

    config.WithEntityFrameworkPersistence();
    config.EnableOutbox();
    config.EnableScheduler();
    config.EnableTracing();
    config.EnableInbox();
    config.EnableDeadLetter();
});

builder.Services.AddSignalR();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.MapHub<MessagingHub>("/hubs/messaging");

app.MapGet("/", () => "Peculiar ERP Nexus Admin Dashboard");

app.Run();
