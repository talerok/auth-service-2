using FluentValidation;

namespace Auth.Application.ApiClients.Commands.UpdateApiClient;

public sealed class UpdateApiClientCommandValidator : AbstractValidator<UpdateApiClientCommand>
{
    public UpdateApiClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
