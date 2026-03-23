using System.Collections.Concurrent;
using Vox.Application.Abstractions;

namespace Vox.Infrastructure.Services;

public sealed class PresenceService : IPresenceService
{
    // connectionId -> userId
    private readonly ConcurrentDictionary<string, string> _connectionUserMap = new();

    // connectionId -> set of channelIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connectionChannelMap = new();

    // connectionId -> last heartbeat timestamp (UTC)
    private readonly ConcurrentDictionary<string, DateTimeOffset> _heartbeatMap = new();

    // userId -> set of connectionIds (a user can have multiple connections)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnectionMap = new();

    // Per-user locks to ensure atomic connect/disconnect for the same user
    private readonly ConcurrentDictionary<string, object> _userLocks = new();

    private readonly TimeProvider _timeProvider;

    private object GetUserLock(string userId) => _userLocks.GetOrAdd(userId, _ => new object());

    public PresenceService() : this(TimeProvider.System) { }

    public PresenceService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task UserConnectedAsync(string connectionId, string userId)
    {
        _connectionUserMap[connectionId] = userId;
        _connectionChannelMap[connectionId] = new ConcurrentDictionary<string, byte>();
        _heartbeatMap[connectionId] = _timeProvider.GetUtcNow();

        var userLock = GetUserLock(userId);
        lock (userLock)
        {
            if (!_userConnectionMap.TryGetValue(userId, out var connections))
            {
                connections = new ConcurrentDictionary<string, byte>();
                _userConnectionMap[userId] = connections;
            }
            connections.TryAdd(connectionId, 0);
        }

        return Task.CompletedTask;
    }

    public Task UserDisconnectedAsync(string connectionId)
    {
        _connectionUserMap.TryRemove(connectionId, out var userId);
        _connectionChannelMap.TryRemove(connectionId, out _);
        _heartbeatMap.TryRemove(connectionId, out _);

        if (userId is not null)
        {
            var userLock = GetUserLock(userId);
            lock (userLock)
            {
                if (_userConnectionMap.TryGetValue(userId, out var connections))
                {
                    connections.TryRemove(connectionId, out _);
                    if (connections.IsEmpty)
                    {
                        _userConnectionMap.TryRemove(userId, out _);
                        _userLocks.TryRemove(userId, out _);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UserJoinedChannelAsync(string connectionId, string channelId)
    {
        if (_connectionChannelMap.TryGetValue(connectionId, out var channels))
        {
            channels.TryAdd(channelId, 0);
        }

        return Task.CompletedTask;
    }

    public Task UserLeftChannelAsync(string connectionId, string channelId)
    {
        if (_connectionChannelMap.TryGetValue(connectionId, out var channels))
        {
            channels.TryRemove(channelId, out _);
        }

        return Task.CompletedTask;
    }

    public Task HeartbeatAsync(string connectionId)
    {
        _heartbeatMap.AddOrUpdate(connectionId, _ => _timeProvider.GetUtcNow(), (_, _) => _timeProvider.GetUtcNow());

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetOnlineUserIdsForServer(IReadOnlyList<Guid> memberUserIds)
    {
        var memberIdStrings = new HashSet<string>(memberUserIds.Select(id => id.ToString()));

        return _userConnectionMap.Keys
            .Where(uid => memberIdStrings.Contains(uid))
            .Distinct()
            .ToList();
    }

    public IReadOnlyList<string> GetOnlineUserIdsForChannel(string channelId)
    {
        var userIds = new HashSet<string>();

        foreach (var (connectionId, channels) in _connectionChannelMap)
        {
            if (channels.ContainsKey(channelId) &&
                _connectionUserMap.TryGetValue(connectionId, out var userId))
            {
                userIds.Add(userId);
            }
        }

        return userIds.ToList();
    }

    public IReadOnlyList<string> GetStaleConnectionIds(TimeSpan timeout)
    {
        var cutoff = _timeProvider.GetUtcNow() - timeout;

        return _heartbeatMap
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public bool IsConnectionStale(string connectionId, TimeSpan timeout)
    {
        if (!_heartbeatMap.TryGetValue(connectionId, out var lastHeartbeat))
        {
            return true;
        }

        return lastHeartbeat < _timeProvider.GetUtcNow() - timeout;
    }

    public string? GetUserIdByConnectionId(string connectionId)
    {
        _connectionUserMap.TryGetValue(connectionId, out var userId);
        return userId;
    }

    public IReadOnlyList<string> GetChannelsByConnectionId(string connectionId)
    {
        if (_connectionChannelMap.TryGetValue(connectionId, out var channels))
        {
            return channels.Keys.ToList();
        }

        return [];
    }

    public bool IsUserOnline(string userId)
    {
        return _userConnectionMap.TryGetValue(userId, out var connections) && !connections.IsEmpty;
    }

    public bool IsUserInChannel(string userId, string channelId)
    {
        if (!_userConnectionMap.TryGetValue(userId, out var connectionIds))
        {
            return false;
        }

        foreach (var connectionId in connectionIds.Keys)
        {
            if (_connectionChannelMap.TryGetValue(connectionId, out var channels) &&
                channels.ContainsKey(channelId))
            {
                return true;
            }
        }

        return false;
    }
}
