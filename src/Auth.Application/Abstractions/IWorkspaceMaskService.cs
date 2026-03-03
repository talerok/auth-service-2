namespace Auth.Application;

public interface IWorkspaceMaskService
{
    Task<Dictionary<string, byte[]>> BuildWorkspaceMasksAsync(Guid userId, CancellationToken cancellationToken);
    Task<Dictionary<string, byte[]>> BuildApiClientWorkspaceMasksAsync(Guid apiClientId, CancellationToken cancellationToken);
}
