using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Vox.Application.Abstractions;
using Vox.Infrastructure.Hubs;
using Vox.Infrastructure.Services;

namespace Vox.Infrastructure.Tests;

public class PresenceHeartbeatServiceTests
{
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock = new();
    private readonly Mock<ILogger<PresenceHeartbeatService>> _loggerMock = new();
    private readonly Mock<IHubClients> _hubClientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly PresenceHeartbeatService _sut;

    public PresenceHeartbeatServiceTests()
    {
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _sut = new PresenceHeartbeatService(
            _presenceServiceMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CleanupStaleConnections_DisconnectsStaleConnection()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string> { "conn1" });
        _presenceServiceMock.Setup(p => p.IsConnectionStale("conn1", It.IsAny<TimeSpan>()))
            .Returns(true);
        _presenceServiceMock.Setup(p => p.GetUserIdByConnectionId("conn1"))
            .Returns("user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string>());
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(true);

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync("conn1"), Times.Once);
    }

    [Fact]
    public async Task CleanupStaleConnections_NotifiesChannelAboutOfflineUser()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string> { "conn1" });
        _presenceServiceMock.Setup(p => p.IsConnectionStale("conn1", It.IsAny<TimeSpan>()))
            .Returns(true);
        _presenceServiceMock.Setup(p => p.GetUserIdByConnectionId("conn1"))
            .Returns("user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string> { "channel1", "channel2" });
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(true);
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", It.IsAny<string>()))
            .Returns(false);

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert - should notify both channels
        _hubClientsMock.Verify(c => c.Group("channel1"), Times.Once);
        _hubClientsMock.Verify(c => c.Group("channel2"), Times.Once);
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("UserStatusChanged",
                It.Is<object[]>(args => args.Length == 2 &&
                    (string)args[0] == "user1" &&
                    (string)args[1] == "Offline"),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CleanupStaleConnections_DoesNotNotify_WhenUserStillInChannel()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string> { "conn1" });
        _presenceServiceMock.Setup(p => p.IsConnectionStale("conn1", It.IsAny<TimeSpan>()))
            .Returns(true);
        _presenceServiceMock.Setup(p => p.GetUserIdByConnectionId("conn1"))
            .Returns("user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string> { "channel1" });
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(false);
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", "channel1"))
            .Returns(true);

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert - should not send UserStatusChanged because user is still in channel
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("UserStatusChanged",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupStaleConnections_SkipsConnection_WhenReCheckShowsFresh()
    {
        // Arrange - GetStaleConnectionIds returns conn1, but re-check shows it's no longer stale
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string> { "conn1" });
        _presenceServiceMock.Setup(p => p.IsConnectionStale("conn1", It.IsAny<TimeSpan>()))
            .Returns(false);

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert - should not disconnect because re-check showed fresh
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CleanupStaleConnections_DoesNothing_WhenNoStaleConnections()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string>());

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CleanupStaleConnections_HandlesNullUserId()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string> { "conn1" });
        _presenceServiceMock.Setup(p => p.IsConnectionStale("conn1", It.IsAny<TimeSpan>()))
            .Returns(true);
        _presenceServiceMock.Setup(p => p.GetUserIdByConnectionId("conn1"))
            .Returns((string?)null);
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string>());
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(true);

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert - disconnected but no channel notifications
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync("conn1"), Times.Once);
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupStaleConnections_ProcessesMultipleStaleConnections()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string> { "conn1", "conn2" });
        _presenceServiceMock.Setup(p => p.IsConnectionStale(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(true);
        _presenceServiceMock.Setup(p => p.GetUserIdByConnectionId(It.IsAny<string>()))
            .Returns("user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId(It.IsAny<string>()))
            .Returns(new List<string>());
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _sut.CleanupStaleConnectionsAsync();

        // Assert
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync("conn1"), Times.Once);
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync("conn2"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_StopsWhenCancelled()
    {
        // Arrange
        _presenceServiceMock.Setup(p => p.GetStaleConnectionIds(It.IsAny<TimeSpan>()))
            .Returns(new List<string>());

        using var cts = new CancellationTokenSource();

        // Act - start the service and cancel immediately
        await _sut.StartAsync(cts.Token);
        cts.Cancel();
        await _sut.StopAsync(CancellationToken.None);

        // Assert - no exception thrown, service stopped gracefully
    }
}
