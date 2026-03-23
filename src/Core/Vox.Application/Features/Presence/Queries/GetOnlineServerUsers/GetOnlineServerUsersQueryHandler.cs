using MediatR;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Presence.Queries.GetOnlineServerUsers;

public sealed class GetOnlineServerUsersQueryHandler
    : IRequestHandler<GetOnlineServerUsersQuery, IReadOnlyList<PresenceUserDto>>
{
    private readonly IPresenceService _presenceService;
    private readonly IUnitOfWork _unitOfWork;

    public GetOnlineServerUsersQueryHandler(IPresenceService presenceService, IUnitOfWork unitOfWork)
    {
        _presenceService = presenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PresenceUserDto>> Handle(
        GetOnlineServerUsersQuery request,
        CancellationToken cancellationToken)
    {
        var server = await _unitOfWork.Servers.GetByIdAsync(request.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{request.ServerId}' was not found.");

        var memberUserIds = server.Members.Select(m => m.UserId).ToList();
        var onlineUserIds = _presenceService.GetOnlineUserIdsForServer(memberUserIds);

        var results = new List<PresenceUserDto>();
        foreach (var uid in onlineUserIds)
        {
            if (Guid.TryParse(uid, out var userGuid))
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userGuid, cancellationToken);
                results.Add(new PresenceUserDto(uid, "Online", user?.DisplayName));
            }
            else
            {
                results.Add(new PresenceUserDto(uid, "Online"));
            }
        }

        return results;
    }
}
