namespace Auth.Domain;

public sealed class Permission : EntityBase
{
    public string Domain { get; init; } = string.Empty;
    public int Bit { get; init; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; init; }
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    public void GuardNotSystem()
    {
        if (IsSystem)
            throw new DomainException("SYSTEM_ENTITY_MODIFICATION_FORBIDDEN");
    }
}
