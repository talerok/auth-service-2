namespace Auth.Domain;

public sealed class IdentitySourceLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid IdentitySourceId { get; set; }
    public string ExternalIdentity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
