namespace Auth.Domain;

public sealed class ServiceAccountWorkspaceRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceAccountWorkspaceId { get; set; }
    public Guid RoleId { get; set; }
    public ServiceAccountWorkspace? ServiceAccountWorkspace { get; set; }
    public Role? Role { get; set; }
}
