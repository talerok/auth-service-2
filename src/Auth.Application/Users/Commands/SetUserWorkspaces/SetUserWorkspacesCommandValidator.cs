using FluentValidation;

namespace Auth.Application.Users.Commands.SetUserWorkspaces;

public sealed class SetUserWorkspacesCommandValidator : AbstractValidator<SetUserWorkspacesCommand>
{
    public SetUserWorkspacesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
