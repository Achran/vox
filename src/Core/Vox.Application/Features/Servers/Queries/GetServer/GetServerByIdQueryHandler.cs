using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Servers.Queries.GetServer;

public sealed class GetServerByIdQueryHandler : IRequestHandler<GetServerByIdQuery, ServerDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServerDto?> Handle(GetServerByIdQuery request, CancellationToken cancellationToken)
    {
        var server = await _unitOfWork.Servers.GetByIdAsync(request.ServerId, cancellationToken);

        if (server is null)
        {
            return null;
        }

        return new ServerDto(
            server.Id,
            server.Name,
            server.Description,
            server.IconUrl,
            server.OwnerId,
            server.CreatedAt
        );
    }
}
