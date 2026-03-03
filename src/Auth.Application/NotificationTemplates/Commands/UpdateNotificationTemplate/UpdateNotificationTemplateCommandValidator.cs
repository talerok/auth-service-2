using FluentValidation;

namespace Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandValidator : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Channel).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
    }
}
