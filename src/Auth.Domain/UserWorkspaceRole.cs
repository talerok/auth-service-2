namespace Auth.Domain;

public sealed class UserWorkspaceRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserWorkspaceId { get; set; }
    public Guid RoleId { get; set; }
    public UserWorkspace? UserWorkspace { get; set; }
    public Role? Role { get; set; }
}
