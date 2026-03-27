using FluentValidation;
using static Auth.Application.Applications.UriValidation;

namespace Auth.Application.Applications.Commands.PatchApplication;

public sealed class PatchApplicationCommandValidator : AbstractValidator<PatchApplicationCommand>
{
    public PatchApplicationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name.Value!).NotNull().MaximumLength(200)
            .OverridePropertyName("Name").When(x => x.Name.HasValue);
        RuleFor(x => x.Description.Value!).NotNull().MaximumLength(500)
            .OverridePropertyName("Description").When(x => x.Description.HasValue);

        RuleFor(x => x.RedirectUris.Value).NotNull()
            .OverridePropertyName("RedirectUris").When(x => x.RedirectUris.HasValue);
        RuleForEach(x => x.RedirectUris.Value!)
            .Must(BeValidRedirectUri).WithMessage("Each redirect URI must be a valid absolute URI with https scheme (or http://localhost for development).")
            .OverridePropertyName("RedirectUris").When(x => x.RedirectUris.HasValue);

        RuleFor(x => x.PostLogoutRedirectUris.Value).NotNull()
            .OverridePropertyName("PostLogoutRedirectUris").When(x => x.PostLogoutRedirectUris.HasValue);
        RuleForEach(x => x.PostLogoutRedirectUris.Value!)
            .Must(BeValidAbsoluteUri).WithMessage("Each post-logout redirect URI must be a valid absolute URL.")
            .OverridePropertyName("PostLogoutRedirectUris").When(x => x.PostLogoutRedirectUris.HasValue);

        RuleFor(x => x.AllowedOrigins.Value).NotNull()
            .OverridePropertyName("AllowedOrigins").When(x => x.AllowedOrigins.HasValue);
        RuleForEach(x => x.AllowedOrigins.Value!)
            .Must(BeValidOrigin).WithMessage("Each allowed origin must be a valid http or https URL (e.g. https://example.com).")
            .OverridePropertyName("AllowedOrigins").When(x => x.AllowedOrigins.HasValue);

        RuleFor(x => x.LogoUrl.Value)
            .Must(BeValidAbsoluteUri!).WithMessage("LogoUrl must be a valid absolute URL.")
            .OverridePropertyName("LogoUrl").When(x => x.LogoUrl.HasValue && x.LogoUrl.Value is not null);

        RuleFor(x => x.HomepageUrl.Value)
            .Must(BeValidAbsoluteUri!).WithMessage("HomepageUrl must be a valid absolute URL.")
            .OverridePropertyName("HomepageUrl").When(x => x.HomepageUrl.HasValue && x.HomepageUrl.Value is not null);

        RuleFor(x => x.ConsentType.Value!)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(x => x is "explicit" or "implicit")
            .WithMessage("ConsentType must be 'explicit' or 'implicit'.")
            .OverridePropertyName("ConsentType").When(x => x.ConsentType.HasValue);

        RuleFor(x => x.Scopes.Value).NotNull()
            .OverridePropertyName("Scopes").When(x => x.Scopes.HasValue);

        RuleFor(x => x.GrantTypes.Value).NotNull()
            .OverridePropertyName("GrantTypes").When(x => x.GrantTypes.HasValue);
        RuleFor(x => x.GrantTypes.Value!)
            .Must(gt => gt.Count > 0)
            .WithMessage("Grant types list cannot be empty when provided.")
            .OverridePropertyName("GrantTypes").When(x => x.GrantTypes.HasValue && x.GrantTypes.Value is not null);

        RuleForEach(x => x.GrantTypes.Value!)
            .Must(gt => OidcConstants.AllowedGrantTypes.Contains(gt))
            .WithMessage("Each grant type must be one of: " + string.Join(", ", OidcConstants.AllowedGrantTypes))
            .OverridePropertyName("GrantTypes").When(x => x.GrantTypes.HasValue);

        RuleFor(x => x.Audiences.Value).NotNull()
            .OverridePropertyName("Audiences").When(x => x.Audiences.HasValue);
        RuleForEach(x => x.Audiences.Value!)
            .NotEmpty().MaximumLength(500)
            .OverridePropertyName("Audiences").When(x => x.Audiences.HasValue);

        RuleFor(x => x.AccessTokenLifetimeMinutes.Value)
            .Must(x => x is null or 0 || (x >= 1 && x <= 1440))
            .WithMessage("AccessTokenLifetimeMinutes must be null, 0 (reset to default), or between 1 and 1440.")
            .OverridePropertyName("AccessTokenLifetimeMinutes").When(x => x.AccessTokenLifetimeMinutes.HasValue);

        RuleFor(x => x.RefreshTokenLifetimeMinutes.Value)
            .Must(x => x is null or 0 || (x >= 1 && x <= 43200))
            .WithMessage("RefreshTokenLifetimeMinutes must be null, 0 (reset to default), or between 1 and 43200.")
            .OverridePropertyName("RefreshTokenLifetimeMinutes").When(x => x.RefreshTokenLifetimeMinutes.HasValue);
    }

}
