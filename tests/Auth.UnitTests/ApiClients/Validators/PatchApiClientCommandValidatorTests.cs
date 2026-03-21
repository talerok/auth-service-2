using Auth.Application.ApiClients.Commands.PatchApiClient;
using Auth.Domain;
using FluentAssertions;

namespace Auth.UnitTests.ApiClients.Validators;

public sealed class PatchApiClientCommandValidatorTests
{
    private readonly PatchApiClientCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), "Name", null, null,
            null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new PatchApiClientCommand(Guid.Empty, "Name", null, null,
            null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithAllNullFields_IsValid()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_HttpsRedirectUri_IsValid()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, ["https://example.com/cb"], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_HttpRedirectUri_HasError()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, ["http://example.com/cb"], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_LocalhostHttp_IsValid()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, ["http://localhost:3000/cb"], null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            null, null, null, null, null, null, "wrong");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConsentType");
    }

    [Fact]
    public async Task Validate_ServiceAccount_NotConfidential_HasError()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            ApiClientType.ServiceAccount, false, null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IsConfidential");
    }

    [Fact]
    public async Task Validate_InvalidLogoUrl_HasError()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null,
            null, null, "not-a-url", null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoUrl");
    }
}
