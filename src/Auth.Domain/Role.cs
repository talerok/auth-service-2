namespace Auth.Domain;

public sealed class Role : EntityBase
{
    [Auditable] public string Name { get; set; } = string.Empty;
    [Auditable] public string Code { get; set; } = string.Empty;
    [Auditable] public string Description { get; set; } = string.Empty;
    public ICollection<UserWorkspaceRole> UserWorkspaceRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
