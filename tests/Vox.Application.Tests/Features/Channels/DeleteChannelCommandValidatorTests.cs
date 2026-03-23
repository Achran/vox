using FluentValidation.TestHelper;
using Vox.Application.Features.Channels.Commands.DeleteChannel;

namespace Vox.Application.Tests.Features.Channels;

public class DeleteChannelCommandValidatorTests
{
    private readonly DeleteChannelCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new DeleteChannelCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyChannelId_FailsValidation()
    {
        var command = new DeleteChannelCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ChannelId);
    }

    [Fact]
    public void Validate_WithEmptyRequestingUserId_FailsValidation()
    {
        var command = new DeleteChannelCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RequestingUserId);
    }
}
