using FluentValidation;

namespace Auth.Application.NotificationTemplates.Commands.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandValidator : AbstractValidator<CreateNotificationTemplateCommand>
{
    public CreateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Type).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Subject).MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
    }
}
