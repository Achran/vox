using System.Collections.Concurrent;
using Vox.Application.Abstractions;

namespace Vox.Infrastructure.Services;

public sealed class VoiceSessionService : IVoiceSessionService
{
    // channelId -> set of (userId, connectionId)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>> _channels = new();

    // connectionId -> set of channelIds (for fast cleanup on disconnect)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connectionChannels = new();

    // Per-channel locks for atomic operations
    private readonly ConcurrentDictionary<string, object> _channelLocks = new();

    private object GetChannelLock(string channelId) => _channelLocks.GetOrAdd(channelId, _ => new object());

    public bool JoinChannel(string channelId, string userId, string connectionId)
    {
        var channelLock = GetChannelLock(channelId);
        bool isNewParticipant;

        lock (channelLock)
        {
            var channelUsers = _channels.GetOrAdd(channelId, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>());
            var userConnections = channelUsers.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            isNewParticipant = userConnections.IsEmpty;
            userConnections.TryAdd(connectionId, 0);
        }

        // Track reverse mapping
        var connChannels = _connectionChannels.GetOrAdd(connectionId, _ => new ConcurrentDictionary<string, byte>());
        connChannels.TryAdd(channelId, 0);

        return isNewParticipant;
    }

    public bool LeaveChannel(string channelId, string userId, string connectionId)
    {
        var channelLock = GetChannelLock(channelId);
        bool isLastConnection = false;

        lock (channelLock)
        {
            if (_channels.TryGetValue(channelId, out var channelUsers) &&
                channelUsers.TryGetValue(userId, out var userConnections))
            {
                userConnections.TryRemove(connectionId, out _);
                if (userConnections.IsEmpty)
                {
                    channelUsers.TryRemove(userId, out _);
                    isLastConnection = true;
                }

                if (channelUsers.IsEmpty)
                {
                    _channels.TryRemove(channelId, out _);
                    _channelLocks.TryRemove(channelId, out _);
                }
            }
        }

        // Update reverse mapping
        if (_connectionChannels.TryGetValue(connectionId, out var connChannels))
        {
            connChannels.TryRemove(channelId, out _);
        }

        return isLastConnection;
    }

    public IReadOnlyList<string> RemoveConnection(string connectionId)
    {
        if (!_connectionChannels.TryRemove(connectionId, out var connChannels))
        {
            return [];
        }

        var channelIds = connChannels.Keys.ToList();

        foreach (var channelId in channelIds)
        {
            var channelLock = GetChannelLock(channelId);
            lock (channelLock)
            {
                if (_channels.TryGetValue(channelId, out var channelUsers))
                {
                    // Find and remove this connection from whichever user owns it
                    foreach (var (userId, userConnections) in channelUsers)
                    {
                        userConnections.TryRemove(connectionId, out _);
                        if (userConnections.IsEmpty)
                        {
                            channelUsers.TryRemove(userId, out _);
                        }
                    }

                    if (channelUsers.IsEmpty)
                    {
                        _channels.TryRemove(channelId, out _);
                        _channelLocks.TryRemove(channelId, out _);
                    }
                }
            }
        }

        return channelIds;
    }

    public IReadOnlyList<string> GetParticipants(string channelId)
    {
        if (_channels.TryGetValue(channelId, out var channelUsers))
        {
            return channelUsers.Keys.ToList();
        }

        return [];
    }

    public bool IsUserInVoiceChannel(string channelId, string userId)
    {
        return _channels.TryGetValue(channelId, out var channelUsers) &&
               channelUsers.TryGetValue(userId, out var connections) &&
               !connections.IsEmpty;
    }
}
