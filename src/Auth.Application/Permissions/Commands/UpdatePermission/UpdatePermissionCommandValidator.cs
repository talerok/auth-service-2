using FluentValidation;

namespace Auth.Application.Permissions.Commands.UpdatePermission;

public sealed class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
