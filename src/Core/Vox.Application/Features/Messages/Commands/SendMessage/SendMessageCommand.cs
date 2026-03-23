using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Messages.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ChannelId,
    string Content,
    Guid AuthorId
) : IRequest<MessageDto>;
