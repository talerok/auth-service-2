namespace Auth.Domain;

public sealed class ApiClient : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ApiClientType Type { get; set; } = ApiClientType.ServiceAccount;
    public bool IsConfidential { get; set; } = true;
    public string? LogoUrl { get; set; }
    public string? HomepageUrl { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public ICollection<ApiClientWorkspace> ApiClientWorkspaces { get; set; } = [];
}
