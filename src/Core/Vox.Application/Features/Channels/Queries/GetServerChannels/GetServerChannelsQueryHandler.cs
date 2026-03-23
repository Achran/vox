using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Channels.Queries.GetServerChannels;

public sealed class GetServerChannelsQueryHandler : IRequestHandler<GetServerChannelsQuery, IReadOnlyList<ChannelDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServerChannelsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ChannelDto>> Handle(GetServerChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = await _unitOfWork.Channels.GetByServerIdAsync(request.ServerId, cancellationToken);

        return channels
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.Type.ToString(),
                c.ServerId,
                c.CreatedAt
            ))
            .ToList();
    }
}
