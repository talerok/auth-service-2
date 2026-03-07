using FluentValidation;

namespace Auth.Application.Users.Commands.SetUserIdentitySourceLinks;

public sealed class SetUserIdentitySourceLinksCommandValidator : AbstractValidator<SetUserIdentitySourceLinksCommand>
{
    public SetUserIdentitySourceLinksCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(x => x.IdentitySourceId).NotEmpty();
            link.RuleFor(x => x.ExternalIdentity).NotEmpty();
        });
    }
}
