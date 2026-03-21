namespace Auth.Domain;

public sealed class ServiceAccount : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ServiceAccountWorkspace> ServiceAccountWorkspaces { get; set; } = [];
}
