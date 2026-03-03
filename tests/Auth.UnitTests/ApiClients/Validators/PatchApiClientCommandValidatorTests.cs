using Auth.Application.ApiClients.Commands.PatchApiClient;
using FluentAssertions;

namespace Auth.UnitTests.ApiClients.Validators;

public sealed class PatchApiClientCommandValidatorTests
{
    private readonly PatchApiClientCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), "Name", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new PatchApiClientCommand(Guid.Empty, "Name", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithAllNullFields_IsValid()
    {
        var command = new PatchApiClientCommand(Guid.NewGuid(), null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
