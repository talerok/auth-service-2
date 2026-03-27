using FluentValidation;

namespace Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;

public sealed class PatchNotificationTemplateCommandValidator : AbstractValidator<PatchNotificationTemplateCommand>
{
    public PatchNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Type.Value!).NotNull().MaximumLength(32)
            .OverridePropertyName("Type").When(x => x.Type.HasValue);
        RuleFor(x => x.Locale.Value!).NotNull().MaximumLength(16)
            .OverridePropertyName("Locale").When(x => x.Locale.HasValue);
        RuleFor(x => x.Subject.Value!).NotNull().MaximumLength(500)
            .OverridePropertyName("Subject").When(x => x.Subject.HasValue);
        RuleFor(x => x.Body.Value!).NotNull()
            .OverridePropertyName("Body").When(x => x.Body.HasValue);
    }
}
