using FluentValidation.TestHelper;
using Vox.Application.Features.Servers.Commands.DeleteServer;

namespace Vox.Application.Tests.Features.Servers;

public class DeleteServerCommandValidatorTests
{
    private readonly DeleteServerCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new DeleteServerCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyServerId_FailsValidation()
    {
        var command = new DeleteServerCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServerId);
    }

    [Fact]
    public void Validate_WithEmptyRequestingUserId_FailsValidation()
    {
        var command = new DeleteServerCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RequestingUserId);
    }
}
