using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Queries.GetLinkedProviders;

public sealed class GetLinkedProvidersQueryHandler
    : IRequestHandler<GetLinkedProvidersQuery, IReadOnlyList<LinkedAccountDto>>
{
    private readonly IIdentityService _identityService;

    public GetLinkedProvidersQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<IReadOnlyList<LinkedAccountDto>> Handle(
        GetLinkedProvidersQuery request,
        CancellationToken cancellationToken)
        => _identityService.GetLinkedProvidersAsync(request.UserId, cancellationToken);
}
