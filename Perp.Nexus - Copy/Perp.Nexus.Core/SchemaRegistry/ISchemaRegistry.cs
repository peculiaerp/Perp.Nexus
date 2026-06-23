namespace Perp.Nexus.Core.SchemaRegistry;

public interface ISchemaRegistry
{
    Task RegisterAsync<T>(int version = 1, CancellationToken cancellationToken = default);
    Task<MessageSchema?> GetSchemaAsync(string type, int version, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync<T>(T message, int version = 1, CancellationToken cancellationToken = default);
    Task<List<MessageSchema>> GetSchemasForTypeAsync(string type, CancellationToken cancellationToken = default);
}

public sealed class MessageSchema
{
    public required string Type { get; init; }
    public int Version { get; init; } = 1;
    public required string SchemaJson { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
