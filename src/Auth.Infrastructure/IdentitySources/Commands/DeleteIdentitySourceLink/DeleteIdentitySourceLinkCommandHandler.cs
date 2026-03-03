using Auth.Application;
using Auth.Application.IdentitySources.Commands.DeleteIdentitySourceLink;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Commands.DeleteIdentitySourceLink;

internal sealed class DeleteIdentitySourceLinkCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<DeleteIdentitySourceLinkCommand>
{
    public async Task Handle(DeleteIdentitySourceLinkCommand command, CancellationToken cancellationToken)
    {
        var link = await dbContext.IdentitySourceLinks
            .FirstOrDefaultAsync(x => x.Id == command.LinkId && x.IdentitySourceId == command.IdentitySourceId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceLinkNotFound);

        dbContext.IdentitySourceLinks.Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
