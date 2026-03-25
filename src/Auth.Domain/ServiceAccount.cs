namespace Auth.Domain;

public sealed class ServiceAccount : EntityBase
{
    [Auditable] public string Name { get; set; } = string.Empty;
    [Auditable] public string Description { get; set; } = string.Empty;
    [Auditable] public string ClientId { get; set; } = string.Empty;
    [Auditable] public bool IsActive { get; set; } = true;
    [Auditable] public List<string> Audiences { get; private set; } = [];
    [Auditable] public int? AccessTokenLifetimeMinutes { get; set; }
    public ICollection<ServiceAccountWorkspace> ServiceAccountWorkspaces { get; set; } = [];

    public void SetAudiences(List<string> audiences) => Audiences = audiences;
}
