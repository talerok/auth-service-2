using Auth.Application;
using Auth.Application.Applications.Commands.SoftDeleteApplication;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Applications.Commands.SoftDeleteApplication;

internal sealed class SoftDeleteApplicationCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<SoftDeleteApplicationCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (application is null)
            return false;

        application.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(application.ClientId, cancellationToken);
        if (oidcApp is not null)
            await appManager.DeleteAsync(oidcApp, cancellationToken);

        await searchIndexService.DeleteApplicationAsync(application.Id, cancellationToken);
        return true;
    }
}
