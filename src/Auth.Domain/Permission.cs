namespace Auth.Domain;

public sealed class Permission : EntityBase
{
    public int Bit { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
