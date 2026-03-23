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

    // Per-connection locks to coordinate join/leave/disconnect for the same connection
    private readonly ConcurrentDictionary<string, object> _connectionLocks = new();

    private object GetChannelLock(string channelId) => _channelLocks.GetOrAdd(channelId, _ => new object());

    private object GetConnectionLock(string connectionId) => _connectionLocks.GetOrAdd(connectionId, _ => new object());

    public bool JoinChannel(string channelId, string userId, string connectionId)
    {
        var channelLock = GetChannelLock(channelId);
        var connectionLock = GetConnectionLock(connectionId);
        bool isNewParticipant;

        // Lock connection first, then channel. This order prevents deadlocks when
        // multiple threads operate on the same connection across different channels,
        // because RemoveConnection also acquires the connection lock first.
        lock (connectionLock)
        {
            lock (channelLock)
            {
                var channelUsers = _channels.GetOrAdd(channelId, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>());
                var userConnections = channelUsers.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
                isNewParticipant = userConnections.IsEmpty;
                userConnections.TryAdd(connectionId, 0);

                // Track reverse mapping atomically with channel membership
                var connChannels = _connectionChannels.GetOrAdd(connectionId, _ => new ConcurrentDictionary<string, byte>());
                connChannels.TryAdd(channelId, 0);
            }
        }

        return isNewParticipant;
    }

    public bool LeaveChannel(string channelId, string userId, string connectionId)
    {
        var channelLock = GetChannelLock(channelId);
        var connectionLock = GetConnectionLock(connectionId);
        bool isLastConnection = false;

        // Lock on connection first, then channel, to match JoinChannel/RemoveConnection
        lock (connectionLock)
        {
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

                // Update reverse mapping under the same critical section
                if (_connectionChannels.TryGetValue(connectionId, out var connChannels))
                {
                    connChannels.TryRemove(channelId, out _);
                    if (connChannels.IsEmpty)
                    {
                        _connectionChannels.TryRemove(connectionId, out _);
                        _connectionLocks.TryRemove(connectionId, out _);
                    }
                }
            }
        }

        return isLastConnection;
    }

    public IReadOnlyList<string> RemoveConnection(string connectionId)
    {
        var connectionLock = GetConnectionLock(connectionId);

        // Serialize all operations for this connectionId
        lock (connectionLock)
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

            // Clean up the connection lock since this connection is no longer tracked
            _connectionLocks.TryRemove(connectionId, out _);

            return channelIds;
        }
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
