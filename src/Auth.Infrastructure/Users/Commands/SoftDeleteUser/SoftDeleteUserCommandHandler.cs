using Auth.Application;
using Auth.Application.Users.Commands.SoftDeleteUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.SoftDeleteUser;

internal sealed class SoftDeleteUserCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteUserCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (user is null)
            return false;

        auditContext.Details = new Dictionary<string, object?> { ["username"] = user.Username };
        user.SoftDelete();
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeleteUserAsync(command.Id, cancellationToken);
        return true;
    }
}
