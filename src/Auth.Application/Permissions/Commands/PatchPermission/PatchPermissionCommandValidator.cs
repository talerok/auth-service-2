using FluentValidation;

namespace Auth.Application.Permissions.Commands.PatchPermission;

public sealed class PatchPermissionCommandValidator : AbstractValidator<PatchPermissionCommand>
{
    public PatchPermissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
