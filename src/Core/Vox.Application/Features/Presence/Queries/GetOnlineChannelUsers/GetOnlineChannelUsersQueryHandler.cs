using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Presence.Queries.GetOnlineChannelUsers;

public sealed class GetOnlineChannelUsersQueryHandler
    : IRequestHandler<GetOnlineChannelUsersQuery, IReadOnlyList<PresenceUserDto>>
{
    private readonly IPresenceService _presenceService;
    private readonly IUnitOfWork _unitOfWork;

    public GetOnlineChannelUsersQueryHandler(IPresenceService presenceService, IUnitOfWork unitOfWork)
    {
        _presenceService = presenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PresenceUserDto>> Handle(
        GetOnlineChannelUsersQuery request,
        CancellationToken cancellationToken)
    {
        _ = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken)
            ?? throw new KeyNotFoundException($"Channel with ID '{request.ChannelId}' was not found.");

        var onlineUserIds = _presenceService.GetOnlineUserIdsForChannel(request.ChannelId.ToString());

        var tasks = onlineUserIds.Select(async uid =>
        {
            if (Guid.TryParse(uid, out var userGuid))
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userGuid, cancellationToken);
                return new PresenceUserDto(uid, "Online", user?.DisplayName);
            }
            else
            {
                return new PresenceUserDto(uid, "Online");
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}
