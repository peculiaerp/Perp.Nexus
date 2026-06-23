namespace Perp.Nexus.Core.Messages;

public sealed record EventEnvelope
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public required string Type { get; init; }
    public int Version { get; init; } = 1;
    public required string Payload { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public EventEnvelope WithCorrelationId(Guid correlationId)
        => this with { CorrelationId = correlationId };

    public EventEnvelope WithCausationId(Guid causationId)
        => this with { CausationId = causationId };

    public EventEnvelope WithHeader(string key, string value)
    {
        var newHeaders = new Dictionary<string, string>(Headers) { [key] = value };
        return this with { Headers = newHeaders };
    }
}
