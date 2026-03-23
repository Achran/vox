using Vox.Infrastructure.Services;

namespace Vox.Infrastructure.Tests;

public class PresenceServiceTests
{
    private readonly PresenceService _sut = new();

    [Fact]
    public async Task UserConnectedAsync_TracksUser()
    {
        // Act
        await _sut.UserConnectedAsync("conn1", "user1");

        // Assert
        var userId = _sut.GetUserIdByConnectionId("conn1");
        Assert.Equal("user1", userId);
    }

    [Fact]
    public async Task UserDisconnectedAsync_RemovesConnection()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");

        // Act
        await _sut.UserDisconnectedAsync("conn1");

        // Assert
        var userId = _sut.GetUserIdByConnectionId("conn1");
        Assert.Null(userId);
    }

    [Fact]
    public async Task UserJoinedChannelAsync_TracksChannelMembership()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");

        // Act
        await _sut.UserJoinedChannelAsync("conn1", "channel1");

        // Assert
        var channels = _sut.GetChannelsByConnectionId("conn1");
        Assert.Single(channels);
        Assert.Contains("channel1", channels);
    }

    [Fact]
    public async Task UserLeftChannelAsync_RemovesChannelMembership()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");
        await _sut.UserJoinedChannelAsync("conn1", "channel1");

        // Act
        await _sut.UserLeftChannelAsync("conn1", "channel1");

        // Assert
        var channels = _sut.GetChannelsByConnectionId("conn1");
        Assert.Empty(channels);
    }

    [Fact]
    public async Task GetOnlineUserIdsForChannel_ReturnsUsersInChannel()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");
        await _sut.UserConnectedAsync("conn2", "user2");
        await _sut.UserJoinedChannelAsync("conn1", "channel1");
        await _sut.UserJoinedChannelAsync("conn2", "channel1");

        // Act
        var onlineUsers = _sut.GetOnlineUserIdsForChannel("channel1");

        // Assert
        Assert.Equal(2, onlineUsers.Count);
        Assert.Contains("user1", onlineUsers);
        Assert.Contains("user2", onlineUsers);
    }

    [Fact]
    public async Task GetOnlineUserIdsForChannel_DoesNotReturnUsersInOtherChannels()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");
        await _sut.UserConnectedAsync("conn2", "user2");
        await _sut.UserJoinedChannelAsync("conn1", "channel1");
        await _sut.UserJoinedChannelAsync("conn2", "channel2");

        // Act
        var onlineUsers = _sut.GetOnlineUserIdsForChannel("channel1");

        // Assert
        Assert.Single(onlineUsers);
        Assert.Contains("user1", onlineUsers);
    }

    [Fact]
    public async Task GetOnlineUserIdsForServer_ReturnsOnlineMembers()
    {
        // Arrange
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();

        await _sut.UserConnectedAsync("conn1", memberId1.ToString());
        await _sut.UserConnectedAsync("conn2", nonMemberId.ToString());

        // Act
        var onlineUsers = _sut.GetOnlineUserIdsForServer(
            Guid.NewGuid(),
            new List<Guid> { memberId1, memberId2 });

        // Assert
        Assert.Single(onlineUsers);
        Assert.Contains(memberId1.ToString(), onlineUsers);
    }

    [Fact]
    public async Task HeartbeatAsync_UpdatesTimestamp()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");
        await Task.Delay(50);

        // Act
        await _sut.HeartbeatAsync("conn1");

        // Assert - connection should not be stale
        var stale = _sut.GetStaleConnectionIds(TimeSpan.FromSeconds(30));
        Assert.Empty(stale);
    }

    [Fact]
    public async Task GetStaleConnectionIds_ReturnsStaleConnections()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");

        // Act - use very short timeout to simulate staleness
        await Task.Delay(50);
        var stale = _sut.GetStaleConnectionIds(TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Single(stale);
        Assert.Contains("conn1", stale);
    }

    [Fact]
    public async Task GetStaleConnectionIds_ExcludesFreshConnections()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");

        // Act
        var stale = _sut.GetStaleConnectionIds(TimeSpan.FromMinutes(5));

        // Assert
        Assert.Empty(stale);
    }

    [Fact]
    public async Task MultipleConnections_SameUser_TrackedCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _sut.UserConnectedAsync("conn1", userId.ToString());
        await _sut.UserConnectedAsync("conn2", userId.ToString());

        // Act - disconnect first connection
        await _sut.UserDisconnectedAsync("conn1");

        // Assert - user is still online via second connection
        var onlineUsers = _sut.GetOnlineUserIdsForServer(
            Guid.NewGuid(),
            new List<Guid> { userId });
        Assert.Single(onlineUsers);

        // Act - disconnect second connection
        await _sut.UserDisconnectedAsync("conn2");

        // Assert - user is now offline
        onlineUsers = _sut.GetOnlineUserIdsForServer(
            Guid.NewGuid(),
            new List<Guid> { userId });
        Assert.Empty(onlineUsers);
    }

    [Fact]
    public async Task DisconnectedUser_RemovedFromChannels()
    {
        // Arrange
        await _sut.UserConnectedAsync("conn1", "user1");
        await _sut.UserJoinedChannelAsync("conn1", "channel1");

        // Act
        await _sut.UserDisconnectedAsync("conn1");

        // Assert
        var channelUsers = _sut.GetOnlineUserIdsForChannel("channel1");
        Assert.Empty(channelUsers);
    }

    [Fact]
    public void GetChannelsByConnectionId_ReturnsEmptyForUnknownConnection()
    {
        // Act
        var channels = _sut.GetChannelsByConnectionId("unknown");

        // Assert
        Assert.Empty(channels);
    }

    [Fact]
    public void GetUserIdByConnectionId_ReturnsNullForUnknownConnection()
    {
        // Act
        var userId = _sut.GetUserIdByConnectionId("unknown");

        // Assert
        Assert.Null(userId);
    }
}
