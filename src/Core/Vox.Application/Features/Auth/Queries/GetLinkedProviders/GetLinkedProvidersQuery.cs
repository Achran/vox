using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Queries.GetLinkedProviders;

public sealed record GetLinkedProvidersQuery(
    string UserId
) : IRequest<IReadOnlyList<LinkedAccountDto>>;
