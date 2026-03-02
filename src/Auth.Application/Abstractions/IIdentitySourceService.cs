namespace Auth.Application;

public interface IIdentitySourceService
{
    Task<IReadOnlyCollection<IdentitySourceDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<IdentitySourceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentitySourceDetailDto> CreateAsync(CreateIdentitySourceRequest request, CancellationToken cancellationToken);
    Task<IdentitySourceDetailDto> UpdateAsync(Guid id, UpdateIdentitySourceRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<IdentitySourceLinkDto>> GetLinksAsync(Guid identitySourceId, CancellationToken cancellationToken);
    Task<IdentitySourceLinkDto> CreateLinkAsync(Guid identitySourceId, CreateIdentitySourceLinkRequest request, CancellationToken cancellationToken);
    Task DeleteLinkAsync(Guid identitySourceId, Guid linkId, CancellationToken cancellationToken);
}
