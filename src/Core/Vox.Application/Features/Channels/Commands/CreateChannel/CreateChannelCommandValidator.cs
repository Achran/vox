using FluentValidation;

namespace Vox.Application.Features.Channels.Commands.CreateChannel;

public sealed class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.ServerId)
            .NotEmpty().WithMessage("Server ID is required.");

        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Channel name is required.")
            .MaximumLength(100).WithMessage("Channel name must not exceed 100 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Channel type is required.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");
    }
}
