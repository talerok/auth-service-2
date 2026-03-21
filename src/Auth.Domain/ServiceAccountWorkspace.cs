namespace Auth.Domain;

public sealed class ServiceAccountWorkspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceAccountId { get; set; }
    public Guid WorkspaceId { get; set; }
    public ServiceAccount? ServiceAccount { get; set; }
    public Workspace? Workspace { get; set; }
    public ICollection<ServiceAccountWorkspaceRole> ServiceAccountWorkspaceRoles { get; set; } = [];
}
