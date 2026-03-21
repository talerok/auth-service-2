using Auth.Application.ApiClients.Commands.UpdateApiClient;
using Auth.Domain;
using FluentAssertions;

namespace Auth.UnitTests.ApiClients.Validators;

public sealed class UpdateApiClientCommandValidatorTests
{
    private readonly UpdateApiClientCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "Name", "Desc", true,
            ApiClientType.ServiceAccount, true, null, null, [], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.Empty, "Name", "Desc", true,
            ApiClientType.ServiceAccount, true, null, null, [], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithEmptyName_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "", "Desc", true,
            ApiClientType.ServiceAccount, true, null, null, [], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_OAuthApp_WithRedirectUris_IsValid()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "OAuth", "desc", true,
            ApiClientType.OAuthApplication, true, null, null,
            ["https://example.com/callback"], [], "explicit");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_OAuthApp_WithoutRedirectUris_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "OAuth", "desc", true,
            ApiClientType.OAuthApplication, true, null, null, [], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RedirectUris");
    }

    [Fact]
    public async Task Validate_ServiceAccount_NotConfidential_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "SA", "desc", true,
            ApiClientType.ServiceAccount, false, null, null, [], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IsConfidential");
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "App", "desc", true,
            ApiClientType.OAuthApplication, true, null, null,
            ["https://example.com/cb"], [], "wrong");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConsentType");
    }

    [Fact]
    public async Task Validate_HttpRedirectUri_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "App", "desc", true,
            ApiClientType.OAuthApplication, true, null, null,
            ["http://example.com/callback"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_LocalhostHttpRedirectUri_IsValid()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "App", "desc", true,
            ApiClientType.OAuthApplication, true, null, null,
            ["http://localhost:5000/callback"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
