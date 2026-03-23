using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Channels.Commands.CreateChannel;

public sealed class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, ChannelDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateChannelCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChannelDto> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var server = await _unitOfWork.Servers.GetByIdAsync(request.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{request.ServerId}' was not found.");

        if (server.OwnerId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Only the server owner can create channels.");
        }

        if (!Enum.TryParse<ChannelType>(request.Type, ignoreCase: true, out var channelType))
        {
            throw new InvalidOperationException($"Invalid channel type '{request.Type}'. Valid types are: Text, Voice.");
        }

        var channel = server.AddChannel(request.Name, channelType);
        await _unitOfWork.Servers.UpdateAsync(server, cancellationToken);
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
