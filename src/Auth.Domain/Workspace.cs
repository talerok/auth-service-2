namespace Auth.Domain;

public sealed class Workspace : EntityBase
{
    [Auditable] public string Name { get; set; } = string.Empty;
    [Auditable] public string Code { get; set; } = string.Empty;
    [Auditable] public string Description { get; set; } = string.Empty;
    [Auditable] public bool IsSystem { get; init; }
    public ICollection<UserWorkspace> UserWorkspaces { get; private set; } = [];

    public void GuardNotSystem()
    {
        if (IsSystem)
            throw new DomainException("SYSTEM_ENTITY_MODIFICATION_FORBIDDEN");
    }
}
