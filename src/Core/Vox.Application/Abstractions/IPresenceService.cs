namespace Vox.Application.Abstractions;

public interface IPresenceService
{
    Task UserConnectedAsync(string connectionId, string userId);
    Task UserDisconnectedAsync(string connectionId);
    Task UserJoinedChannelAsync(string connectionId, string channelId);
    Task UserLeftChannelAsync(string connectionId, string channelId);
    Task HeartbeatAsync(string connectionId);
    IReadOnlyList<string> GetOnlineUserIdsForServer(IReadOnlyList<Guid> memberUserIds);
    IReadOnlyList<string> GetOnlineUserIdsForChannel(string channelId);
    IReadOnlyList<string> GetStaleConnectionIds(TimeSpan timeout);
    bool IsConnectionStale(string connectionId, TimeSpan timeout);
    string? GetUserIdByConnectionId(string connectionId);
    IReadOnlyList<string> GetChannelsByConnectionId(string connectionId);
    bool IsUserOnline(string userId);
    bool IsUserInChannel(string userId, string channelId);
}
