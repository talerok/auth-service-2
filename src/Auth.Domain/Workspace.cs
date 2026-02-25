namespace Auth.Domain;

public sealed class Workspace : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public ICollection<UserWorkspace> UserWorkspaces { get; set; } = [];
}
