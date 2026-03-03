using FluentValidation;

namespace Auth.Application.Roles.Commands.PatchRole;

public sealed class PatchRoleCommandValidator : AbstractValidator<PatchRoleCommand>
{
    public PatchRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
