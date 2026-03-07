using Auth.Application;
using Auth.Application.Roles.Commands.CreateRole;
using Auth.Domain;
using MediatR;

namespace Auth.Infrastructure.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = new Role { Name = command.Name, Code = command.Code, Description = command.Description };
        dbContext.Roles.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Code, entity.Description);
        await searchIndexService.IndexRoleAsync(dto, cancellationToken);
        return dto;
    }
}
