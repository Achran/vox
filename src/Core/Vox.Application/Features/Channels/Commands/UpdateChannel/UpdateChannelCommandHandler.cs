using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Channels.Commands.UpdateChannel;

public sealed class UpdateChannelCommandHandler : IRequestHandler<UpdateChannelCommand, ChannelDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateChannelCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChannelDto> Handle(UpdateChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken)
            ?? throw new KeyNotFoundException($"Channel with ID '{request.ChannelId}' was not found.");

        var server = await _unitOfWork.Servers.GetByIdAsync(channel.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{channel.ServerId}' was not found.");

        if (server.OwnerId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Only the server owner can update channels.");
        }

        channel.UpdateName(request.Name);
        await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChannelDto(
            channel.Id,
            channel.Name,
            channel.Type.ToString(),
            channel.ServerId,
            channel.CreatedAt
        );
    }
}
