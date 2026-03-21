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
            true, null, null, ["https://example.com/cb"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.Empty, "Name", "Desc", true,
            true, null, null, ["https://example.com/cb"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithEmptyName_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "", "Desc", true,
            true, null, null, ["https://example.com/cb"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithRedirectUris_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "OAuth", "desc", true,
            true, null, null,
            ["https://example.com/callback"], [], "explicit");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithoutRedirectUris_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "OAuth", "desc", true,
            true, null, null, [], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RedirectUris");
    }

    [Fact]
    public async Task Validate_InvalidConsentType_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            true, null, null,
            ["https://example.com/cb"], [], "wrong");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConsentType");
    }

    [Fact]
    public async Task Validate_HttpRedirectUri_HasError()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            true, null, null,
            ["http://example.com/callback"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RedirectUris"));
    }

    [Fact]
    public async Task Validate_LocalhostHttpRedirectUri_IsValid()
    {
        var command = new UpdateApplicationCommand(Guid.NewGuid(), "App", "desc", true,
            true, null, null,
            ["http://localhost:5000/callback"], [], null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
