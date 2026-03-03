using Auth.Application.ApiClients.Commands.CreateApiClient;
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
}
