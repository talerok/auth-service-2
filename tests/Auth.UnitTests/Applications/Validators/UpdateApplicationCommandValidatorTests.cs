using Auth.Application.Applications.Commands.UpdateApplication;
using FluentAssertions;

namespace Auth.UnitTests.Applications.Validators;

public sealed class UpdateApplicationCommandValidatorTests
{
    private readonly UpdateApplicationCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "Name", "Desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.Empty, "Name", "Desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithEmptyName_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "", "Desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithRedirectUris_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "OAuth", "desc", true,
            null, null,
            ["https://example.com/callback"], [], [], "explicit", [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithoutRedirectUris_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "OAuth", "desc", true,
            null, null, [], [], [], null, [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RedirectUris");
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null,
            ["https://example.com/cb"], [], [], "wrong", [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConsentType");
    }

    [Fact]
    public async Task Validate_HttpRedirectUri_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null,
            ["http://example.com/callback"], [], [], null, [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_LocalhostHttpRedirectUri_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null,
            ["http://localhost:5000/callback"], [], [], null, [], ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidAllowedOrigin_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], ["https://example.com"], null, [],
            ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAllowedOriginContainingPath_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], ["https://example.com/some/path"], null, [],
            ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AllowedOrigins"));
    }

    [Fact]
    public async Task Validate_WithAllowedOriginNotUrl_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], ["not-a-url"], null, [],
            ["authorization_code", "refresh_token"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AllowedOrigins"));
    }

    [Fact]
    public async Task Validate_WithEmptyGrantTypes_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [], [], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GrantTypes");
    }

    [Fact]
    public async Task Validate_WithInvalidGrantType_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [], ["invalid"], [], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("GrantTypes"));
    }

    [Fact]
    public async Task Validate_WithValidTokenLifetimes_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [],
            ["authorization_code", "refresh_token"], [], 30, 10080);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAccessTokenLifetimeOutOfRange_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [],
            ["authorization_code", "refresh_token"], [], 1441, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccessTokenLifetimeMinutes");
    }

    [Fact]
    public async Task Validate_WithRefreshTokenLifetimeOutOfRange_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            null, null, ["https://example.com/cb"], [], [], null, [],
            ["authorization_code", "refresh_token"], [], null, 43201);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshTokenLifetimeMinutes");
    }
}
