using FluentValidation.TestHelper;
using Vox.Application.Features.Servers.Commands.CreateServer;

namespace Vox.Application.Tests.Features.Servers;

public class CreateServerCommandValidatorTests
{
    private readonly CreateServerCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new CreateServerCommand("My Server", "A description", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var command = new CreateServerCommand("", null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithWhitespaceName_FailsValidation()
    {
        var command = new CreateServerCommand("   ", null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_FailsValidation()
    {
        var command = new CreateServerCommand(new string('a', 101), null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithEmptyOwnerId_FailsValidation()
    {
        var command = new CreateServerCommand("My Server", null, Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OwnerId);
    }
}
