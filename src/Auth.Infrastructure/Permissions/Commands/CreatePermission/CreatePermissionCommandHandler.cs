using Auth.Application;
using Auth.Application.Permissions.Commands.CreatePermission;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.CreatePermission;

internal sealed class CreatePermissionCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<CreatePermissionCommand, PermissionDto>
{
    public async Task<PermissionDto> Handle(CreatePermissionCommand command, CancellationToken cancellationToken)
    {
        var maxBit = await dbContext.Permissions.IgnoreQueryFilters()
            .Select(x => (int?)x.Bit)
            .MaxAsync(cancellationToken) ?? -1;

        var entity = new Permission
        {
            Bit = Math.Max(maxBit + 1, SystemPermissionCatalog.CustomBitStart),
            Code = command.Code,
            Description = command.Description,
            IsSystem = false
        };
        dbContext.Permissions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new PermissionDto(entity.Id, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
        await searchIndexService.IndexPermissionAsync(dto, cancellationToken);
        return dto;
    }
}
