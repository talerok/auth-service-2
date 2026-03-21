using Auth.Application.ApiClients.Commands.CreateApiClient;
using Auth.Domain;
using FluentAssertions;

namespace Auth.UnitTests.ApiClients.Validators;

public sealed class CreateApiClientCommandValidatorTests
{
    private readonly CreateApiClientCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new CreateApiClientCommand("My Client", "Description");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_HasError(string? name)
    {
        var command = new CreateApiClientCommand(name!, "desc");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_OAuthApp_WithRedirectUris_IsValid()
    {
        var command = new CreateApiClientCommand("OAuth App", "desc",
            Type: ApiClientType.OAuthApplication,
            IsConfidential: true,
            RedirectUris: ["https://example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_OAuthApp_WithoutRedirectUris_HasError()
    {
        var command = new CreateApiClientCommand("OAuth App", "desc",
            Type: ApiClientType.OAuthApplication,
            IsConfidential: true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RedirectUris");
    }

    [Fact]
    public async Task Validate_OAuthApp_WithHttpRedirectUri_HasError()
    {
        var command = new CreateApiClientCommand("OAuth App", "desc",
            Type: ApiClientType.OAuthApplication,
            RedirectUris: ["http://example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_OAuthApp_WithLocalhostHttp_IsValid()
    {
        var command = new CreateApiClientCommand("OAuth App", "desc",
            Type: ApiClientType.OAuthApplication,
            RedirectUris: ["http://localhost:3000/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ServiceAccount_WithIsConfidentialFalse_HasError()
    {
        var command = new CreateApiClientCommand("SA", "desc",
            Type: ApiClientType.ServiceAccount,
            IsConfidential: false);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IsConfidential");
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new CreateApiClientCommand("App", "desc",
            Type: ApiClientType.OAuthApplication,
            RedirectUris: ["https://example.com/cb"],
            ConsentType: "invalid");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConsentType");
    }

    [Theory]
    [InlineData("explicit")]
    [InlineData("implicit")]
    public async Task Validate_ValidConsentType_IsValid(string consentType)
    {
        var command = new CreateApiClientCommand("App", "desc",
            Type: ApiClientType.OAuthApplication,
            RedirectUris: ["https://example.com/cb"],
            ConsentType: consentType);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidLogoUrl_HasError()
    {
        var command = new CreateApiClientCommand("App", "desc",
            LogoUrl: "not-a-url");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoUrl");
    }

    [Fact]
    public async Task Validate_OAuthApp_PublicClient_IsValid()
    {
        var command = new CreateApiClientCommand("SPA App", "desc",
            Type: ApiClientType.OAuthApplication,
            IsConfidential: false,
            RedirectUris: ["https://spa.example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
