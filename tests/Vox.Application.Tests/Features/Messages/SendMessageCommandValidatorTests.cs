using FluentValidation.TestHelper;
using Vox.Application.Features.Messages.Commands.SendMessage;

namespace Vox.Application.Tests.Features.Messages;

public class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new SendMessageCommand(Guid.NewGuid(), "Hello, world!", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyChannelId_FailsValidation()
    {
        var command = new SendMessageCommand(Guid.Empty, "Hello!", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ChannelId);
    }

    [Fact]
    public void Validate_WithEmptyAuthorId_FailsValidation()
    {
        var command = new SendMessageCommand(Guid.NewGuid(), "Hello!", Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AuthorId);
    }

    [Fact]
    public void Validate_WithEmptyContent_FailsValidation()
    {
        var command = new SendMessageCommand(Guid.NewGuid(), "", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WithContentExceeding4000Characters_FailsValidation()
    {
        var longContent = new string('a', 4001);
        var command = new SendMessageCommand(Guid.NewGuid(), longContent, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WithContentAtExactly4000Characters_PassesValidation()
    {
        var content = new string('a', 4000);
        var command = new SendMessageCommand(Guid.NewGuid(), content, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
