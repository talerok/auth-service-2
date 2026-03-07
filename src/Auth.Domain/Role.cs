namespace Auth.Domain;

public sealed class Role : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<UserWorkspaceRole> UserWorkspaceRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
