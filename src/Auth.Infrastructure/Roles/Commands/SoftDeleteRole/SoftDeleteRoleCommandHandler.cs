using Auth.Application;
using Auth.Application.Roles.Commands.SoftDeleteRole;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.SoftDeleteRole;

internal sealed class SoftDeleteRoleCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<SoftDeleteRoleCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.SoftDelete();
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeleteRoleAsync(command.Id, cancellationToken);
        return true;
    }
}
