using MediatR;

namespace Auth.Application.Roles.Commands.SoftDeleteRole;

public sealed record SoftDeleteRoleCommand(Guid Id) : IRequest<bool>;
