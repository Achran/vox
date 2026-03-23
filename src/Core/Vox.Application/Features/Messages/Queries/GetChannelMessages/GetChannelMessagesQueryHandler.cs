using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Messages.Queries.GetChannelMessages;

public sealed class GetChannelMessagesQueryHandler : IRequestHandler<GetChannelMessagesQuery, IReadOnlyList<MessageDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetChannelMessagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<MessageDto>> Handle(GetChannelMessagesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var messages = await _unitOfWork.Messages.GetByChannelIdAsync(
            request.ChannelId, pageSize, request.Before, cancellationToken);

        return messages
            .Select(m => new MessageDto(
                m.Id,
                m.AuthorId,
                m.ChannelId,
                m.Content,
                m.IsEdited,
                m.CreatedAt
            ))
            .ToList();
    }
}
