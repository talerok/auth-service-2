using FluentValidation;

namespace Auth.Application.Roles.Commands.SetRolePermissions;

public sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
