using Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;
using FluentAssertions;

namespace Auth.UnitTests.ServiceAccounts.Validators;

public sealed class PatchServiceAccountCommandValidatorTests
{
    private readonly PatchServiceAccountCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_IsValid()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), "Name", default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyId_HasError()
    {
        var command = new PatchServiceAccountCommand(Guid.Empty, "Name", default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_WithAllNullFields_IsValid()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), default, default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNameExceedingMaxLength_HasError()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), new string('a', 201), default, default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithDescriptionExceedingMaxLength_HasError()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), default, new string('a', 501), default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Description"));
    }

    [Fact]
    public async Task Validate_WithValidDescription_IsValid()
    {
        var command = new PatchServiceAccountCommand(Guid.NewGuid(), default, "updated description", default, default, default);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
