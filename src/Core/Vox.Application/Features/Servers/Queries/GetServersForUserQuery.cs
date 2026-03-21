using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Repositories;

namespace Vox.Application.Features.Servers.Queries;

public record GetServersForUserQuery(Guid UserId) : IRequest<IEnumerable<ServerDto>>;

public class GetServersForUserQueryHandler(IServerRepository serverRepository)
    : IRequestHandler<GetServersForUserQuery, IEnumerable<ServerDto>>
{
    public async Task<IEnumerable<ServerDto>> Handle(GetServersForUserQuery request, CancellationToken cancellationToken)
    {
        var servers = await serverRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return servers.Select(s => new ServerDto(
            s.Id,
            s.Name,
            s.Description,
            s.IconUrl,
            s.OwnerId));
    }
}
