using Perp.Nexus.AdminDashboard.Hubs;
using Perp.Nexus.Core.DependencyInjection;
using Perp.Nexus.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNexusCore()
    .AddNexusInfrastructure(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Nexus;Trusted_Connection=True;";
        options.TransportType = builder.Configuration.GetValue<string>("Transport:Type") ?? "InMemory";
        options.UseOutbox = true;
        options.UseScheduler = true;
        options.UseTracing = true;
        options.UseInboxDeduplication = true;
        options.UseDeadLetter = true;
    });

builder.Services.AddSignalR();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.MapHub<MessagingHub>("/hubs/messaging");

app.MapGet("/", () => "Nexus Admin Dashboard");

app.Run();
