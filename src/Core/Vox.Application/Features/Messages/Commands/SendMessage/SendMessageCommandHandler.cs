using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Messages.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken)
            ?? throw new KeyNotFoundException($"Channel with ID '{request.ChannelId}' was not found.");

        var message = Message.Create(request.AuthorId, request.ChannelId, request.Content);
        await _unitOfWork.Messages.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto(
            message.Id,
            message.AuthorId,
            message.ChannelId,
            message.Content,
            message.IsEdited,
            message.CreatedAt
        );
    }
}
