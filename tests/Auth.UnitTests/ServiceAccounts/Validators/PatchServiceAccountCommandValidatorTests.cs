using Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;
using FluentAssertions;

namespace Auth.UnitTests.ServiceAccounts.Validators;

public sealed class PatchServiceAccountCommandValidatorTests
{
    private readonly PatchServiceAccountCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), "Name", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new PatchServiceAccountCommand(Guid.Empty, "Name", null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithAllNullFields_IsValid()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNameExceedingMaxLength_HasError()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), new string('a', 201), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithDescriptionExceedingMaxLength_HasError()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), null, new string('a', 501), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Validate_WithValidDescription_IsValid()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), null, "updated description", null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
