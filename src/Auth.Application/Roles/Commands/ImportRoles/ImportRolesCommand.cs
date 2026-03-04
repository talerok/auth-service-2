using MediatR;

namespace Auth.Application.Roles.Commands.ImportRoles;

public sealed record ImportRolesCommand(IReadOnlyCollection<ImportRoleItem> Items) : IRequest<ImportRolesResult>;
