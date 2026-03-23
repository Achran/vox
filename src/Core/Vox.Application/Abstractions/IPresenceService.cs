namespace Vox.Application.Abstractions;

public interface IPresenceService
{
    /// <summary>Returns true if this was the user's first connection (they were offline before).</summary>
    Task<bool> UserConnectedAsync(string connectionId, string userId);
    /// <summary>Returns true if this was the user's last connection (they are now offline).</summary>
    Task<bool> UserDisconnectedAsync(string connectionId);
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
