using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Presence.Queries.GetOnlineChannelUsers;

public sealed class GetOnlineChannelUsersQueryHandler
    : IRequestHandler<GetOnlineChannelUsersQuery, IReadOnlyList<PresenceUserDto>>
{
    private readonly IPresenceService _presenceService;

    public GetOnlineChannelUsersQueryHandler(IPresenceService presenceService)
    {
        _presenceService = presenceService;
    }

    public Task<IReadOnlyList<PresenceUserDto>> Handle(
        GetOnlineChannelUsersQuery request,
        CancellationToken cancellationToken)
    {
        var onlineUserIds = _presenceService.GetOnlineUserIdsForChannel(request.ChannelId.ToString());

        if (onlineUserIds is null)
        {
            throw new KeyNotFoundException($"Channel with ID '{request.ChannelId}' was not found.");
        }
        IReadOnlyList<PresenceUserDto> result = onlineUserIds
            .Select(uid => new PresenceUserDto(uid, "Online"))
            .ToList();

        return Task.FromResult(result);
    }
}
