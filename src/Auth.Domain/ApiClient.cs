namespace Auth.Domain;

public sealed class ApiClient : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ApiClientWorkspace> ApiClientWorkspaces { get; set; } = [];
}
