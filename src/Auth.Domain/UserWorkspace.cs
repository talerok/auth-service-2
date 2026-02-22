namespace Auth.Domain;

public sealed class UserWorkspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public User? User { get; set; }
    public Workspace? Workspace { get; set; }
    public ICollection<UserWorkspaceRole> UserWorkspaceRoles { get; set; } = [];
}
