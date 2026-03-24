namespace Auth.Domain;

public sealed class ServiceAccount : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Audiences { get; private set; } = [];
    public int? AccessTokenLifetimeMinutes { get; set; }
    public ICollection<ServiceAccountWorkspace> ServiceAccountWorkspaces { get; set; } = [];

    public void SetAudiences(List<string> audiences) => Audiences = audiences;
}
