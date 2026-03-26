using FluentValidation;

namespace Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;

public sealed class PatchNotificationTemplateCommandValidator : AbstractValidator<PatchNotificationTemplateCommand>
{
    public PatchNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Type).MaximumLength(32).When(x => x.Type is not null);
        RuleFor(x => x.Locale).MaximumLength(16).When(x => x.Locale is not null);
        RuleFor(x => x.Subject).MaximumLength(500).When(x => x.Subject is not null);
    }
}
