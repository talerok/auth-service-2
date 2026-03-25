namespace Auth.Domain;

public sealed class Permission : EntityBase
{
    [Auditable] public string Domain { get; init; } = string.Empty;
    [Auditable] public int Bit { get; init; }
    [Auditable] public string Code { get; set; } = string.Empty;
    [Auditable] public string Description { get; set; } = string.Empty;
    [Auditable] public bool IsSystem { get; init; }
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    public void GuardNotSystem()
    {
        if (IsSystem)
            throw new DomainException("SYSTEM_ENTITY_MODIFICATION_FORBIDDEN");
    }
}
