namespace Auth.Domain;

public sealed class ApiClientWorkspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApiClientId { get; set; }
    public Guid WorkspaceId { get; set; }
    public ApiClient? ApiClient { get; set; }
    public Workspace? Workspace { get; set; }
    public ICollection<ApiClientWorkspaceRole> ApiClientWorkspaceRoles { get; set; } = [];
}
