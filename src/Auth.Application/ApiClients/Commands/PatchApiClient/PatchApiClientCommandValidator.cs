using Auth.Domain;
using FluentValidation;

namespace Auth.Application.ApiClients.Commands.PatchApiClient;

public sealed class PatchApiClientCommandValidator : AbstractValidator<PatchApiClientCommand>
{
    public PatchApiClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null);

        RuleFor(x => x.IsConfidential)
            .Equal(true)
            .WithMessage("ServiceAccount clients must be confidential.")
            .When(x => x.Type == ApiClientType.ServiceAccount);

        RuleForEach(x => x.RedirectUris)
            .Must(BeValidRedirectUri).WithMessage("Each redirect URI must be a valid absolute URI with https scheme (or http://localhost for development).")
            .When(x => x.RedirectUris is not null);

        RuleForEach(x => x.PostLogoutRedirectUris)
            .Must(BeValidAbsoluteUri).WithMessage("Each post-logout redirect URI must be a valid absolute URL.")
            .When(x => x.PostLogoutRedirectUris is not null);

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
    }

    private static bool BeValidAbsoluteUri(string? uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out _);

    private static bool BeValidRedirectUri(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
        (parsed.Scheme == "https" || (parsed.Scheme == "http" && parsed.Host == "localhost"));
}
