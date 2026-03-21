using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.SoftDeleteServiceAccount;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.SoftDeleteServiceAccount;

internal sealed class SoftDeleteServiceAccountCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IRequestHandler<SoftDeleteServiceAccountCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteServiceAccountCommand command, CancellationToken cancellationToken)
    {
        var serviceAccount = await dbContext.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (serviceAccount is null)
            return false;

        serviceAccount.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(serviceAccount.ClientId, cancellationToken);
        if (oidcApp is not null)
            await appManager.DeleteAsync(oidcApp, cancellationToken);

        await searchIndexService.DeleteServiceAccountAsync(serviceAccount.Id, cancellationToken);
        return true;
    }
}
