using FluentValidation;

namespace Auth.Application.ServiceAccounts.Commands.SetServiceAccountWorkspaces;

public sealed class SetServiceAccountWorkspacesCommandValidator : AbstractValidator<SetServiceAccountWorkspacesCommand>
{
    public SetServiceAccountWorkspacesCommandValidator()
    {
        RuleFor(x => x.ServiceAccountId).NotEmpty();
    }
}
