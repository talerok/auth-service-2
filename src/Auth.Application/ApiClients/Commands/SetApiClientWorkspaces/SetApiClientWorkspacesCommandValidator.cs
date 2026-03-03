using FluentValidation;

namespace Auth.Application.ApiClients.Commands.SetApiClientWorkspaces;

public sealed class SetApiClientWorkspacesCommandValidator : AbstractValidator<SetApiClientWorkspacesCommand>
{
    public SetApiClientWorkspacesCommandValidator()
    {
        RuleFor(x => x.ApiClientId).NotEmpty();
    }
}
