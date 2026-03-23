using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.ExternalLogin;

public sealed class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, AuthTokensDto>
{
    private readonly IIdentityService _identityService;

    public ExternalLoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthTokensDto> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
        => _identityService.ExternalLoginAsync(
            request.Provider,
            request.ProviderKey,
            request.Email,
            request.DisplayName,
            cancellationToken);
}
