namespace Auth.Application;

public interface IApiClientService
{
    Task<IReadOnlyCollection<ApiClientDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ApiClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CreateApiClientResponse> CreateAsync(CreateApiClientRequest request, CancellationToken cancellationToken);
    Task<ApiClientDto?> UpdateAsync(Guid id, UpdateApiClientRequest request, CancellationToken cancellationToken);
    Task<ApiClientDto?> PatchAsync(Guid id, PatchApiClientRequest request, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ApiClientWorkspaceRolesItem>?> GetWorkspacesAsync(Guid apiClientId, CancellationToken cancellationToken);
    Task SetWorkspacesAsync(Guid apiClientId, IReadOnlyCollection<ApiClientWorkspaceRolesItem> workspaces, CancellationToken cancellationToken);
    Task<RegenerateApiClientSecretResponse?> RegenerateSecretAsync(Guid id, CancellationToken cancellationToken);
}
