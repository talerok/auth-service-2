using Auth.Application.Applications.Commands.CreateApplication;
using FluentAssertions;

namespace Auth.UnitTests.Applications.Validators;

public sealed class CreateApplicationCommandValidatorTests
{
    private readonly CreateApplicationCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new CreateApplicationCommand("My Client", "Description",
            RedirectUris: ["https://example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_HasError(string? name)
    {
        var command = new CreateApplicationCommand(name!, "desc",
            RedirectUris: ["https://example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithRedirectUris_IsValid()
    {
        var command = new CreateApplicationCommand("OAuth App", "desc",
            IsConfidential: true,
            RedirectUris: ["https://example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithHttpRedirectUri_HasError()
    {
        var command = new CreateApplicationCommand("OAuth App", "desc",
            RedirectUris: ["http://example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_WithLocalhostHttp_IsValid()
    {
        var command = new CreateApplicationCommand("OAuth App", "desc",
            RedirectUris: ["http://localhost:3000/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new CreateApplicationCommand("App", "desc",
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
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            ConsentType: consentType);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidLogoUrl_HasError()
    {
        var command = new CreateApplicationCommand("App", "desc",
            LogoUrl: "not-a-url",
            RedirectUris: ["https://example.com/cb"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoUrl");
    }

    [Fact]
    public async Task Validate_PublicClient_IsValid()
    {
        var command = new CreateApplicationCommand("SPA App", "desc",
            IsConfidential: false,
            RedirectUris: ["https://spa.example.com/callback"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidGrantTypes_IsValid()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            GrantTypes: ["authorization_code", "refresh_token"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithInvalidGrantType_HasError()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            GrantTypes: ["authorization_code", "invalid_grant"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("GrantTypes"));
    }

    [Fact]
    public async Task Validate_WithNullGrantTypes_IsValid()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            GrantTypes: null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidAccessTokenLifetime_IsValid()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            AccessTokenLifetimeMinutes: 60);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAccessTokenLifetimeTooHigh_HasError()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            AccessTokenLifetimeMinutes: 1441);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessTokenLifetimeMinutes");
    }

    [Fact]
    public async Task Validate_WithAccessTokenLifetimeTooLow_HasError()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            AccessTokenLifetimeMinutes: 0);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessTokenLifetimeMinutes");
    }

    [Fact]
    public async Task Validate_WithRefreshTokenLifetimeTooHigh_HasError()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            RefreshTokenLifetimeMinutes: 43201);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshTokenLifetimeMinutes");
    }

    [Fact]
    public async Task Validate_WithNullLifetimes_IsValid()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            AccessTokenLifetimeMinutes: null,
            RefreshTokenLifetimeMinutes: null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAllGrantTypes_IsValid()
    {
        var command = new CreateApplicationCommand("App", "desc",
            RedirectUris: ["https://example.com/cb"],
            GrantTypes: ["authorization_code", "client_credentials", "refresh_token", "password", "token_exchange", "mfa_otp"]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
