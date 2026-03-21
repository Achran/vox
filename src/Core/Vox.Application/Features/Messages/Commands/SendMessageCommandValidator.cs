using FluentValidation;

namespace Vox.Application.Features.Messages.Commands;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.AuthorId)
            .NotEmpty();

        RuleFor(x => x.ChannelId)
            .NotEmpty();
    }
}
