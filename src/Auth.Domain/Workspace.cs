namespace Auth.Domain;

public sealed class Workspace : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; init; }
    public ICollection<UserWorkspace> UserWorkspaces { get; private set; } = [];

    public void GuardNotSystem()
    {
        if (IsSystem)
            throw new DomainException("SYSTEM_ENTITY_MODIFICATION_FORBIDDEN");
    }
}
