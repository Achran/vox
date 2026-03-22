using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => _identityService.RefreshAsync(request.RefreshToken, cancellationToken);
}
