using FluentValidation;
using static Auth.Application.Applications.UriValidation;

namespace Auth.Application.Applications.Commands.PatchApplication;

public sealed class PatchApplicationCommandValidator : AbstractValidator<PatchApplicationCommand>
{
    public PatchApplicationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);

        RuleForEach(x => x.RedirectUris)
            .Must(BeValidRedirectUri).WithMessage("Each redirect URI must be a valid absolute URI with https scheme (or http://localhost for development).")
            .When(x => x.RedirectUris is not null);

        RuleForEach(x => x.PostLogoutRedirectUris)
            .Must(BeValidAbsoluteUri).WithMessage("Each post-logout redirect URI must be a valid absolute URL.")
            .When(x => x.PostLogoutRedirectUris is not null);

        RuleForEach(x => x.AllowedOrigins)
            .Must(BeValidOrigin).WithMessage("Each allowed origin must be a valid http or https URL (e.g. https://example.com).")
            .When(x => x.AllowedOrigins is not null);

        RuleFor(x => x.LogoUrl)
            .Must(BeValidAbsoluteUri).WithMessage("LogoUrl must be a valid absolute URL.")
            .When(x => x.LogoUrl is not null);

        RuleFor(x => x.HomepageUrl)
            .Must(BeValidAbsoluteUri).WithMessage("HomepageUrl must be a valid absolute URL.")
            .When(x => x.HomepageUrl is not null);

        RuleFor(x => x.ConsentType)
            .Must(x => x is "explicit" or "implicit")
            .WithMessage("ConsentType must be 'explicit' or 'implicit'.")
            .When(x => x.ConsentType is not null);

        RuleFor(x => x.GrantTypes)
            .Must(gt => gt!.Count > 0)
            .WithMessage("Grant types list cannot be empty when provided.")
            .When(x => x.GrantTypes is not null);

        RuleForEach(x => x.GrantTypes)
            .Must(gt => OidcConstants.AllowedGrantTypes.Contains(gt))
            .WithMessage("Each grant type must be one of: " + string.Join(", ", OidcConstants.AllowedGrantTypes))
            .When(x => x.GrantTypes is not null);

        RuleForEach(x => x.Audiences)
            .NotEmpty().MaximumLength(500)
            .When(x => x.Audiences is not null);

        RuleFor(x => x.AccessTokenLifetimeMinutes)
            .Must(x => x == 0 || (x >= 1 && x <= 1440))
            .WithMessage("AccessTokenLifetimeMinutes must be 0 (reset to default) or between 1 and 1440.")
            .When(x => x.AccessTokenLifetimeMinutes.HasValue);

        RuleFor(x => x.RefreshTokenLifetimeMinutes)
            .Must(x => x == 0 || (x >= 1 && x <= 43200))
            .WithMessage("RefreshTokenLifetimeMinutes must be 0 (reset to default) or between 1 and 43200.")
            .When(x => x.RefreshTokenLifetimeMinutes.HasValue);
    }

}
