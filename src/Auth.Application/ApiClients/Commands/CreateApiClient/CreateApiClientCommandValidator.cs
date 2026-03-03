using FluentValidation;

namespace Auth.Application.ApiClients.Commands.CreateApiClient;

public sealed class CreateApiClientCommandValidator : AbstractValidator<CreateApiClientCommand>
{
    public CreateApiClientCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
