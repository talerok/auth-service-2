using Auth.Application;
using Auth.Application.IdentitySources.Commands.DeleteIdentitySource;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Commands.DeleteIdentitySource;

internal sealed class DeleteIdentitySourceCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<DeleteIdentitySourceCommand>
{
    public async Task Handle(DeleteIdentitySourceCommand command, CancellationToken cancellationToken)
    {
        var source = await dbContext.IdentitySources
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        source.SoftDelete();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
