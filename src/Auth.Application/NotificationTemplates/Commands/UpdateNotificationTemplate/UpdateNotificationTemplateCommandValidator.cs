using FluentValidation;

namespace Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandValidator : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Subject).MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
    }
}
