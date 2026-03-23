using MediatR;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Channels.Commands.DeleteChannel;

public sealed class DeleteChannelCommandHandler : IRequestHandler<DeleteChannelCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteChannelCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken)
            ?? throw new KeyNotFoundException($"Channel with ID '{request.ChannelId}' was not found.");

        var server = await _unitOfWork.Servers.GetByIdAsync(channel.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{channel.ServerId}' was not found.");

        if (server.OwnerId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Only the server owner can delete channels.");
        }

        await _unitOfWork.Channels.DeleteAsync(request.ChannelId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
