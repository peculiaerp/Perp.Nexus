using Microsoft.EntityFrameworkCore;
using Perp.Nexus.Core.SchemaRegistry;

namespace Perp.Nexus.Infrastructure.Persistence.EFCore;

internal sealed class SchemaRegistryStore : ISchemaRegistry
{
    private readonly MessagingDbContext _db;

    public SchemaRegistryStore(MessagingDbContext db) => _db = db;

    public async Task RegisterAsync<T>(int version = 1, CancellationToken cancellationToken = default)
    {
        var type = typeof(T).FullName!;
        var schema = System.Text.Json.JsonSerializer.Serialize(typeof(T).GetProperties()
            .ToDictionary(p => p.Name, p => p.PropertyType.Name));

        var existing = await _db.MessageSchemas.FindAsync([type, version], cancellationToken);
        if (existing == null)
        {
            _db.MessageSchemas.Add(new MessageSchema
            {
                Type = type,
                Version = version,
                SchemaJson = schema
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MessageSchema?> GetSchemaAsync(string type, int version, CancellationToken cancellationToken = default)
    {
        return await _db.MessageSchemas.FindAsync([type, version], cancellationToken);
    }

    public async Task<bool> ValidateAsync<T>(T message, int version = 1, CancellationToken cancellationToken = default)
    {
        var schema = await GetSchemaAsync(typeof(T).FullName!, version, cancellationToken);
        return schema != null;
    }

    public async Task<List<MessageSchema>> GetSchemasForTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _db.MessageSchemas
            .Where(x => x.Type == type)
            .OrderBy(x => x.Version)
            .ToListAsync(cancellationToken);
    }
}
