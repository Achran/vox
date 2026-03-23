using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Servers.Queries.GetUserServers;

public sealed record GetUserServersQuery(Guid UserId) : IRequest<IReadOnlyList<ServerDto>>;
