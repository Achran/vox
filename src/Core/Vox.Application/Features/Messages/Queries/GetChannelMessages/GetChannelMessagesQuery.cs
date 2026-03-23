using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Messages.Queries.GetChannelMessages;

public sealed record GetChannelMessagesQuery(
    Guid ChannelId,
    int PageSize = 50,
    DateTime? Before = null
) : IRequest<IReadOnlyList<MessageDto>>;
