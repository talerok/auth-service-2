using FluentValidation;

namespace Auth.Application.IdentitySources.Commands.CreateIdentitySourceLink;

public sealed class CreateIdentitySourceLinkCommandValidator : AbstractValidator<CreateIdentitySourceLinkCommand>
{
    public CreateIdentitySourceLinkCommandValidator()
    {
        RuleFor(x => x.IdentitySourceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ExternalIdentity).NotEmpty();
    }
}
