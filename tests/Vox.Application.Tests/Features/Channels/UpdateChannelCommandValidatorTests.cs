using FluentValidation.TestHelper;
using Vox.Application.Features.Channels.Commands.UpdateChannel;

namespace Vox.Application.Tests.Features.Channels;

public class UpdateChannelCommandValidatorTests
{
    private readonly UpdateChannelCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new UpdateChannelCommand(Guid.NewGuid(), "new-name", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyChannelId_FailsValidation()
    {
        var command = new UpdateChannelCommand(Guid.Empty, "new-name", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ChannelId);
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var command = new UpdateChannelCommand(Guid.NewGuid(), "", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithWhitespaceName_FailsValidation()
    {
        var command = new UpdateChannelCommand(Guid.NewGuid(), "   ", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_FailsValidation()
    {
        var command = new UpdateChannelCommand(Guid.NewGuid(), new string('a', 101), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithEmptyRequestingUserId_FailsValidation()
    {
        var command = new UpdateChannelCommand(Guid.NewGuid(), "new-name", Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RequestingUserId);
    }
}
