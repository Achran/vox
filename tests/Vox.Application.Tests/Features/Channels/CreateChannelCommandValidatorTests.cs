using FluentValidation.TestHelper;
using Vox.Application.Features.Channels.Commands.CreateChannel;

namespace Vox.Application.Tests.Features.Channels;

public class CreateChannelCommandValidatorTests
{
    private readonly CreateChannelCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new CreateChannelCommand(Guid.NewGuid(), "general", "Text", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyServerId_FailsValidation()
    {
        var command = new CreateChannelCommand(Guid.Empty, "general", "Text", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServerId);
    }

    [Fact]
    public void Validate_WithWhitespaceName_FailsValidation()
    {
        var command = new CreateChannelCommand(Guid.NewGuid(), "   ", "Text", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_FailsValidation()
    {
        var command = new CreateChannelCommand(Guid.NewGuid(), new string('a', 101), "Text", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithEmptyType_FailsValidation()
    {
        var command = new CreateChannelCommand(Guid.NewGuid(), "general", "", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WithEmptyRequestingUserId_FailsValidation()
    {
        var command = new CreateChannelCommand(Guid.NewGuid(), "general", "Text", Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RequestingUserId);
    }
}
