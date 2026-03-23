using FluentValidation.TestHelper;
using Vox.Application.Features.Servers.Commands.UpdateServer;

namespace Vox.Application.Tests.Features.Servers;

public class UpdateServerCommandValidatorTests
{
    private readonly UpdateServerCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new UpdateServerCommand(Guid.NewGuid(), "Updated Name", "Description", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyServerId_FailsValidation()
    {
        var command = new UpdateServerCommand(Guid.Empty, "Name", null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServerId);
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var command = new UpdateServerCommand(Guid.NewGuid(), "", null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithWhitespaceName_FailsValidation()
    {
        var command = new UpdateServerCommand(Guid.NewGuid(), "   ", null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_FailsValidation()
    {
        var command = new UpdateServerCommand(Guid.NewGuid(), new string('a', 101), null, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithEmptyRequestingUserId_FailsValidation()
    {
        var command = new UpdateServerCommand(Guid.NewGuid(), "Name", null, Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RequestingUserId);
    }
}
