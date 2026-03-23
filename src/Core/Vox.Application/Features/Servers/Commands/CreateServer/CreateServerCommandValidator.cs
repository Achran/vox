using FluentValidation;

namespace Vox.Application.Features.Servers.Commands.CreateServer;

public sealed class CreateServerCommandValidator : AbstractValidator<CreateServerCommand>
{
    public CreateServerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Server name is required.")
            .MaximumLength(100).WithMessage("Server name must not exceed 100 characters.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Server name must not be empty or whitespace.");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required.");
    }
}
