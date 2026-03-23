using FluentValidation;

namespace Vox.Application.Features.Servers.Commands.UpdateServer;

public sealed class UpdateServerCommandValidator : AbstractValidator<UpdateServerCommand>
{
    public UpdateServerCommandValidator()
    {
        RuleFor(x => x.ServerId)
            .NotEmpty().WithMessage("Server ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Server name is required.")
            .MaximumLength(100).WithMessage("Server name must not exceed 100 characters.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Server name must not be empty or whitespace.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");
    }
}
