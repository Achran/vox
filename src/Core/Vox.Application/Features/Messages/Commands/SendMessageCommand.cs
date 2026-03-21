using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Entities;
using Vox.Domain.Repositories;

namespace Vox.Application.Features.Messages.Commands;

public record SendMessageCommand(
    string Content,
    Guid AuthorId,
    Guid ChannelId) : IRequest<MessageDto>;

public class SendMessageCommandHandler(
    IMessageRepository messageRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SendMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var message = Message.Create(request.Content, request.AuthorId, request.ChannelId);

        await messageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageDto(
            message.Id,
            message.Content,
            message.AuthorId,
            message.ChannelId,
            message.IsEdited,
            message.CreatedAt);
    }
}
