using Auth.Application.Applications.Commands.PatchApplication;
using FluentAssertions;

namespace Auth.UnitTests.Applications.Validators;

public sealed class PatchApplicationCommandValidatorTests
{
    private readonly PatchApplicationCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), "Name", default, default,
            default, default, default, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new PatchApplicationCommand(Guid.Empty, "Name", default, default,
            default, default, default, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithAllNullFields_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_HttpsRedirectUri_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, new List<string> { "https://example.com/cb" }, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_HttpRedirectUri_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, new List<string> { "http://example.com/cb" }, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_LocalhostHttp_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, new List<string> { "http://localhost:3000/cb" }, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidAllowedOrigin_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, new List<string> { "https://example.com" }, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAllowedOriginContainingPath_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, new List<string> { "https://example.com/some/path" }, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AllowedOrigins"));
    }

    [Fact]
    public async Task Validate_WithAllowedOriginNotUrl_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, new List<string> { "not-a-url" }, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AllowedOrigins"));
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, "wrong", default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ConsentType"));
    }

    [Fact]
    public async Task Validate_InvalidLogoUrl_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            "not-a-url", default, default, default, default, default, default, default, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LogoUrl"));
    }

    [Fact]
    public async Task Validate_WithValidGrantTypes_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default,
            new List<string> { "authorization_code", "client_credentials" }, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyGrantTypesList_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, new List<string>(), default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("GrantTypes"));
    }

    [Fact]
    public async Task Validate_WithInvalidGrantType_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, new List<string> { "invalid_grant" }, default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("GrantTypes"));
    }

    [Fact]
    public async Task Validate_WithZeroAccessTokenLifetime_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, default, default, 0, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidAccessTokenLifetime_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, default, default, 30, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNegativeAccessTokenLifetime_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, default, default, -1, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AccessTokenLifetimeMinutes"));
    }

    [Fact]
    public async Task Validate_WithAccessTokenLifetimeTooHigh_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), default, default, default,
            default, default, default, default, default, default, default, default, default, 1441, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AccessTokenLifetimeMinutes"));
    }
}
