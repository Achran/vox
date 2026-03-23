using MediatR;
using Vox.Application.Abstractions;

namespace Vox.Application.Features.Auth.Commands.LinkProvider;

public sealed class LinkProviderCommandHandler : IRequestHandler<LinkProviderCommand>
{
    private readonly IIdentityService _identityService;

    public LinkProviderCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task Handle(LinkProviderCommand request, CancellationToken cancellationToken)
        => _identityService.LinkExternalProviderAsync(
            request.UserId,
            request.Provider,
            request.ProviderKey,
            cancellationToken);
}
