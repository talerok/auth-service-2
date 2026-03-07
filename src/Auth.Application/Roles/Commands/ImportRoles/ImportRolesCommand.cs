using MediatR;

namespace Auth.Application.Roles.Commands.ImportRoles;

public sealed record ImportRolesCommand(IReadOnlyCollection<ImportRoleItem> Items, bool Add = true, bool Edit = true) : IRequest<ImportRolesResult>;
