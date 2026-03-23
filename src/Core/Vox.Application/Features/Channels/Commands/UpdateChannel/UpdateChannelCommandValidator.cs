using FluentValidation;

namespace Vox.Application.Features.Channels.Commands.UpdateChannel;

public sealed class UpdateChannelCommandValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty().WithMessage("Channel ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Channel name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Channel name must not be empty or whitespace.")
            .MaximumLength(100).WithMessage("Channel name must not exceed 100 characters.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");
    }
}
