namespace Auth.Domain;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Topic { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
