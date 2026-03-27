using FluentValidation;

namespace Auth.Application.Permissions.Commands.PatchPermission;

public sealed class PatchPermissionCommandValidator : AbstractValidator<PatchPermissionCommand>
{
    public PatchPermissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code.Value!).NotNull().MaximumLength(200)
            .OverridePropertyName("Code").When(x => x.Code.HasValue);
        RuleFor(x => x.Description.Value!).NotNull().MaximumLength(500)
            .OverridePropertyName("Description").When(x => x.Description.HasValue);
    }
}
