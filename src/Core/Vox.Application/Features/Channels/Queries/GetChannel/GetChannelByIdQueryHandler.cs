using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Channels.Queries.GetChannel;

public sealed class GetChannelByIdQueryHandler : IRequestHandler<GetChannelByIdQuery, ChannelDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetChannelByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChannelDto?> Handle(GetChannelByIdQuery request, CancellationToken cancellationToken)
    {
        var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken);

        if (channel is null)
        {
            return null;
        }

        return new ChannelDto(
            channel.Id,
            channel.Name,
            channel.Type.ToString(),
            channel.ServerId,
            channel.CreatedAt
        );
    }
}
