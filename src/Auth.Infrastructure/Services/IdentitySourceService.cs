using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class IdentitySourceService(AuthDbContext dbContext) : IIdentitySourceService
{
    public async Task<IReadOnlyCollection<IdentitySourceDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.IdentitySources.AsNoTracking()
            .Select(x => new IdentitySourceDto(x.Id, x.Name, x.DisplayName, x.Type, x.IsEnabled, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IdentitySourceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.IdentitySources.AsNoTracking()
            .Include(x => x.OidcConfig)
            .Where(x => x.Id == id)
            .Select(x => new IdentitySourceDetailDto(
                x.Id, x.Name, x.DisplayName, x.Type, x.IsEnabled, x.CreatedAt,
                x.OidcConfig != null
                    ? new IdentitySourceOidcConfigDto(x.OidcConfig.Authority, x.OidcConfig.ClientId, x.OidcConfig.ClientSecret != null)
                    : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IdentitySourceDetailDto> CreateAsync(CreateIdentitySourceRequest request, CancellationToken cancellationToken)
    {
        if (request.Type == IdentitySourceType.Oidc && request.OidcConfig is null)
            throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

        var source = new IdentitySource
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Type = request.Type,
            IsEnabled = true
        };

        if (request.OidcConfig is not null)
        {
            source.OidcConfig = new IdentitySourceOidcConfig
            {
                IdentitySourceId = source.Id,
                Authority = request.OidcConfig.Authority,
                ClientId = request.OidcConfig.ClientId,
                ClientSecret = request.OidcConfig.ClientSecret
            };
        }

        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IdentitySourceDetailDto(
            source.Id, source.Name, source.DisplayName, source.Type, source.IsEnabled, source.CreatedAt,
            source.OidcConfig is not null
                ? new IdentitySourceOidcConfigDto(source.OidcConfig.Authority, source.OidcConfig.ClientId, source.OidcConfig.ClientSecret is not null)
                : null);
    }

    public async Task<IdentitySourceDetailDto> UpdateAsync(Guid id, UpdateIdentitySourceRequest request, CancellationToken cancellationToken)
    {
        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        source.DisplayName = request.DisplayName;
        source.IsEnabled = request.IsEnabled;
        source.UpdatedAt = DateTime.UtcNow;

        if (request.OidcConfig is not null)
        {
            if (source.OidcConfig is not null)
            {
                source.OidcConfig.Authority = request.OidcConfig.Authority;
                source.OidcConfig.ClientId = request.OidcConfig.ClientId;
                if (request.OidcConfig.ClientSecret is not null)
                    source.OidcConfig.ClientSecret = request.OidcConfig.ClientSecret;
            }
            else
            {
                source.OidcConfig = new IdentitySourceOidcConfig
                {
                    IdentitySourceId = source.Id,
                    Authority = request.OidcConfig.Authority,
                    ClientId = request.OidcConfig.ClientId,
                    ClientSecret = request.OidcConfig.ClientSecret
                };
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new IdentitySourceDetailDto(
            source.Id, source.Name, source.DisplayName, source.Type, source.IsEnabled, source.CreatedAt,
            source.OidcConfig is not null
                ? new IdentitySourceOidcConfigDto(source.OidcConfig.Authority, source.OidcConfig.ClientId, source.OidcConfig.ClientSecret is not null)
                : null);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dbContext.IdentitySources
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        source.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<IdentitySourceLinkDto>> GetLinksAsync(Guid identitySourceId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.IdentitySources.AnyAsync(x => x.Id == identitySourceId, cancellationToken);
        if (!exists)
            throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        return await dbContext.IdentitySourceLinks.AsNoTracking()
            .Where(x => x.IdentitySourceId == identitySourceId)
            .Select(x => new IdentitySourceLinkDto(x.Id, x.UserId, x.IdentitySourceId, x.ExternalIdentity, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IdentitySourceLinkDto> CreateLinkAsync(Guid identitySourceId, CreateIdentitySourceLinkRequest request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.IdentitySources.AnyAsync(x => x.Id == identitySourceId, cancellationToken);
        if (!exists)
            throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        var duplicate = await dbContext.IdentitySourceLinks
            .AnyAsync(x => x.IdentitySourceId == identitySourceId && x.ExternalIdentity == request.ExternalIdentity, cancellationToken);
        if (duplicate)
            throw new AuthException(AuthErrorCatalog.IdentitySourceDuplicateLink);

        var link = new IdentitySourceLink
        {
            UserId = request.UserId,
            IdentitySourceId = identitySourceId,
            ExternalIdentity = request.ExternalIdentity
        };

        dbContext.IdentitySourceLinks.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IdentitySourceLinkDto(link.Id, link.UserId, link.IdentitySourceId, link.ExternalIdentity, link.CreatedAt);
    }

    public async Task DeleteLinkAsync(Guid identitySourceId, Guid linkId, CancellationToken cancellationToken)
    {
        var link = await dbContext.IdentitySourceLinks
            .FirstOrDefaultAsync(x => x.Id == linkId && x.IdentitySourceId == identitySourceId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceLinkNotFound);

        dbContext.IdentitySourceLinks.Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
