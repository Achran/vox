using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthTokensDto>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthTokensDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => _identityService.RegisterAsync(
            request.UserName,
            request.Email,
            request.DisplayName,
            request.Password,
            cancellationToken);
}
