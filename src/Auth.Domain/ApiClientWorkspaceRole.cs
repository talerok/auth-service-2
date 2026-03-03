namespace Auth.Domain;

public sealed class ApiClientWorkspaceRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApiClientWorkspaceId { get; set; }
    public Guid RoleId { get; set; }
    public ApiClientWorkspace? ApiClientWorkspace { get; set; }
    public Role? Role { get; set; }
}
