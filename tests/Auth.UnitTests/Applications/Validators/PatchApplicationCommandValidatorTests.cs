using Auth.Application.Applications.Commands.PatchApplication;
using FluentAssertions;

namespace Auth.UnitTests.Applications.Validators;

public sealed class PatchApplicationCommandValidatorTests
{
    private readonly PatchApplicationCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), "Name", null, null,
            null, null, null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new PatchApplicationCommand(Guid.Empty, "Name", null, null,
            null, null, null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithAllNullFields_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_HttpsRedirectUri_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, ["https://example.com/cb"], null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_HttpRedirectUri_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, ["http://example.com/cb"], null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_LocalhostHttp_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, ["http://localhost:3000/cb"], null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, "wrong", null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConsentType");
    }

    [Fact]
    public async Task Validate_InvalidLogoUrl_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            "not-a-url", null, null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoUrl");
    }

    [Fact]
    public async Task Validate_WithValidGrantTypes_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null,
            ["authorization_code", "client_credentials"], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyGrantTypesList_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GrantTypes");
    }

    [Fact]
    public async Task Validate_WithInvalidGrantType_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, ["invalid_grant"], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("GrantTypes"));
    }

    [Fact]
    public async Task Validate_WithZeroAccessTokenLifetime_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, null, 0, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidAccessTokenLifetime_IsValid()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, null, 30, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNegativeAccessTokenLifetime_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, null, -1, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessTokenLifetimeMinutes");
    }

    [Fact]
    public async Task Validate_WithAccessTokenLifetimeTooHigh_HasError()
    {
        var command = new PatchApplicationCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, null, 1441, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessTokenLifetimeMinutes");
    }
}
