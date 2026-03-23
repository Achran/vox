using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Servers.Queries.GetUserServers;

public sealed class GetUserServersQueryHandler : IRequestHandler<GetUserServersQuery, IReadOnlyList<ServerDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserServersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ServerDto>> Handle(GetUserServersQuery request, CancellationToken cancellationToken)
    {
        var servers = await _unitOfWork.Servers.GetByUserIdAsync(request.UserId, cancellationToken);

        return servers
            .Select(s => new ServerDto(
                s.Id,
                s.Name,
                s.Description,
                s.IconUrl,
                s.OwnerId,
                s.CreatedAt
            ))
            .ToList();
    }
}
