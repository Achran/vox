using MediatR;
using Vox.Application.Abstractions;

namespace Vox.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IIdentityService _identityService;

    public RevokeTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        => _identityService.RevokeAsync(request.RefreshToken, cancellationToken);
}
