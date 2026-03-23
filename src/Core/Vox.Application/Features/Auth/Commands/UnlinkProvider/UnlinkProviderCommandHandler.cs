using MediatR;
using Vox.Application.Abstractions;

namespace Vox.Application.Features.Auth.Commands.UnlinkProvider;

public sealed class UnlinkProviderCommandHandler : IRequestHandler<UnlinkProviderCommand>
{
    private readonly IIdentityService _identityService;

    public UnlinkProviderCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task Handle(UnlinkProviderCommand request, CancellationToken cancellationToken)
        => _identityService.UnlinkExternalProviderAsync(
            request.UserId,
            request.Provider,
            cancellationToken);
}
