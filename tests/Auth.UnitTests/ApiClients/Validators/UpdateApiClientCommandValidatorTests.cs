using Auth.Application.ApiClients.Commands.UpdateApiClient;
using FluentAssertions;

namespace Auth.UnitTests.ApiClients.Validators;

public sealed class UpdateApiClientCommandValidatorTests
{
    private readonly UpdateApiClientCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "Name", "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.Empty, "Name", "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithEmptyName_HasError()
    {
        var command = new UpdateApiClientCommand(Guid.NewGuid(), "", "Desc", true);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
