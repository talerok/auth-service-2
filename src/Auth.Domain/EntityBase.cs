namespace Auth.Domain;

public abstract class EntityBase
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    public void SoftDelete() => DeletedAt = DateTime.UtcNow;
    public void Restore() => DeletedAt = null;
}
