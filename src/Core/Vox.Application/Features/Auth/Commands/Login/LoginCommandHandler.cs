using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokensDto>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        => _identityService.LoginAsync(request.Email, request.Password, cancellationToken);
}
