using FluentValidation;

namespace Vox.Application.Features.Channels.Commands.DeleteChannel;

public sealed class DeleteChannelCommandValidator : AbstractValidator<DeleteChannelCommand>
{
    public DeleteChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty().WithMessage("Channel ID is required.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");
    }
}
