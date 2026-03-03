using Auth.Application;
using Auth.Application.Permissions.Commands.UpdatePermission;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.UpdatePermission;

internal sealed class UpdatePermissionCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<UpdatePermissionCommand, PermissionDto?>
{
    public async Task<PermissionDto?> Handle(UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
            return null;

        entity.Description = command.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new PermissionDto(entity.Id, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        return dto;
    }
}
