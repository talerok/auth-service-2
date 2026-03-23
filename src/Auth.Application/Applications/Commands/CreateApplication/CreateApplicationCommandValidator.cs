using FluentValidation;
using static Auth.Application.Applications.UriValidation;

namespace Auth.Application.Applications.Commands.CreateApplication;

public sealed class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);

        RuleFor(x => x.RedirectUris)
            .NotEmpty().WithMessage("At least one redirect URI is required for OAuth applications.");

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

        RuleForEach(x => x.GrantTypes)
            .Must(gt => OidcConstants.AllowedGrantTypes.Contains(gt))
            .WithMessage("Each grant type must be one of: " + string.Join(", ", OidcConstants.AllowedGrantTypes))
            .When(x => x.GrantTypes is not null);

        RuleFor(x => x.AccessTokenLifetimeMinutes)
            .InclusiveBetween(1, 1440)
            .When(x => x.AccessTokenLifetimeMinutes.HasValue);

        RuleFor(x => x.RefreshTokenLifetimeMinutes)
            .InclusiveBetween(1, 43200)
            .When(x => x.RefreshTokenLifetimeMinutes.HasValue);
    }

}
