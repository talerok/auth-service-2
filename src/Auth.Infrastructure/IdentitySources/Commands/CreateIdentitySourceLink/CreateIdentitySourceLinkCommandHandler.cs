using Auth.Application;
using Auth.Application.IdentitySources.Commands.CreateIdentitySourceLink;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Commands.CreateIdentitySourceLink;

internal sealed class CreateIdentitySourceLinkCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<CreateIdentitySourceLinkCommand, IdentitySourceLinkDto>
{
    public async Task<IdentitySourceLinkDto> Handle(CreateIdentitySourceLinkCommand command, CancellationToken cancellationToken)
    {
        var exists = await dbContext.IdentitySources.AnyAsync(x => x.Id == command.IdentitySourceId, cancellationToken);
        if (!exists)
            throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        var duplicate = await dbContext.IdentitySourceLinks
            .AnyAsync(x => x.IdentitySourceId == command.IdentitySourceId && x.ExternalIdentity == command.ExternalIdentity, cancellationToken);
        if (duplicate)
            throw new AuthException(AuthErrorCatalog.IdentitySourceDuplicateLink);

        var link = new IdentitySourceLink
        {
            UserId = command.UserId,
            IdentitySourceId = command.IdentitySourceId,
            ExternalIdentity = command.ExternalIdentity
        };

        dbContext.IdentitySourceLinks.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IdentitySourceLinkDto(link.Id, link.UserId, link.IdentitySourceId, link.ExternalIdentity, link.CreatedAt);
    }
}
