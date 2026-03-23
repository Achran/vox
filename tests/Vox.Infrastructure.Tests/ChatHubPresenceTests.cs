using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Moq;
using MediatR;
using Vox.Application.Abstractions;
using Vox.Infrastructure.Hubs;

namespace Vox.Infrastructure.Tests;

public class ChatHubPresenceTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<HubCallerContext> _contextMock = new();
    private readonly Mock<IHubCallerClients> _clientsMock = new();
    private readonly Mock<IGroupManager> _groupsMock = new();
    private readonly Mock<IClientProxy> _allClientsMock = new();
    private readonly Mock<IClientProxy> _otherClientsMock = new();
    private readonly Mock<IClientProxy> _groupClientsMock = new();
    private readonly ChatHub _hub;

    public ChatHubPresenceTests()
    {
        _clientsMock.Setup(c => c.All).Returns(_allClientsMock.Object);
        _clientsMock.Setup(c => c.Others).Returns(_otherClientsMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_groupClientsMock.Object);

        _hub = new ChatHub(_mediatorMock.Object, _presenceServiceMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object
        };
    }

    private void SetupAuthenticatedUser(string connectionId, string userId, string displayName = "TestUser")
    {
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _contextMock.Setup(c => c.UserIdentifier).Returns(userId);
        _contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("domain_user_id", userId),
            new Claim("display_name", displayName),
            new Claim("unique_name", displayName)
        }, "test")));
    }

    private void SetupUnauthenticatedUser(string connectionId)
    {
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _contextMock.Setup(c => c.UserIdentifier).Returns((string?)null);
        _contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    // --- OnConnectedAsync tests ---

    [Fact]
    public async Task OnConnectedAsync_TracksUserConnection()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.UserConnectedAsync("conn1", "user1"))
            .ReturnsAsync(true);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _presenceServiceMock.Verify(p => p.UserConnectedAsync("conn1", "user1"), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_SendsUserOnline_WhenFirstConnection()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1", "TestUser");
        _presenceServiceMock.Setup(p => p.UserConnectedAsync("conn1", "user1"))
            .ReturnsAsync(true);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _otherClientsMock.Verify(
            c => c.SendCoreAsync("UserOnline",
                It.Is<object[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_DoesNotSendUserOnline_WhenSubsequentConnection()
    {
        // Arrange
        SetupAuthenticatedUser("conn2", "user1");
        _presenceServiceMock.Setup(p => p.UserConnectedAsync("conn2", "user1"))
            .ReturnsAsync(false);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _otherClientsMock.Verify(
            c => c.SendCoreAsync("UserOnline", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnConnectedAsync_DoesNotTrack_WhenUnauthenticated()
    {
        // Arrange
        SetupUnauthenticatedUser("conn1");

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _presenceServiceMock.Verify(
            p => p.UserConnectedAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // --- OnDisconnectedAsync tests ---

    [Fact]
    public async Task OnDisconnectedAsync_RemovesUserConnection()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string>());
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(true);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _presenceServiceMock.Verify(p => p.UserDisconnectedAsync("conn1"), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_SendsUserOffline_WhenLastConnection()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string>());
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(true);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _otherClientsMock.Verify(
            c => c.SendCoreAsync("UserOffline",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "user1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_DoesNotSendUserOffline_WhenOtherConnectionsRemain()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string>());
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(false);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _otherClientsMock.Verify(
            c => c.SendCoreAsync("UserOffline", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_NotifiesChannels_WhenUserLeavesChannel()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string> { "channel1" });
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(true);
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", "channel1"))
            .Returns(false);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _groupClientsMock.Verify(
            c => c.SendCoreAsync("UserStatusChanged",
                It.Is<object[]>(args => args.Length == 2 &&
                    (string)args[0] == "user1" &&
                    (string)args[1] == "Offline"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_DoesNotNotifyChannel_WhenUserStillInChannel()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.GetChannelsByConnectionId("conn1"))
            .Returns(new List<string> { "channel1" });
        _presenceServiceMock.Setup(p => p.UserDisconnectedAsync("conn1"))
            .ReturnsAsync(false);
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", "channel1"))
            .Returns(true);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _groupClientsMock.Verify(
            c => c.SendCoreAsync("UserStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- Heartbeat tests ---

    [Fact]
    public async Task Heartbeat_UpdatesPresenceService()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");

        // Act
        await _hub.Heartbeat();

        // Assert
        _presenceServiceMock.Verify(p => p.HeartbeatAsync("conn1"), Times.Once);
    }

    // --- JoinChannel tests ---

    [Fact]
    public async Task JoinChannel_TracksPresenceAndNotifiesGroup()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", "channel1"))
            .Returns(false);

        // Act
        await _hub.JoinChannel("channel1");

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync("conn1", "channel1", It.IsAny<CancellationToken>()), Times.Once);
        _presenceServiceMock.Verify(p => p.UserJoinedChannelAsync("conn1", "channel1"), Times.Once);
        _groupClientsMock.Verify(
            c => c.SendCoreAsync("UserJoined",
                It.Is<object[]>(args => args.Length == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinChannel_SendsUserStatusOnline_WhenFirstConnectionInChannel()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", "channel1"))
            .Returns(false);

        // Act
        await _hub.JoinChannel("channel1");

        // Assert
        _groupClientsMock.Verify(
            c => c.SendCoreAsync("UserStatusChanged",
                It.Is<object[]>(args => args.Length == 2 &&
                    (string)args[0] == "user1" &&
                    (string)args[1] == "Online"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinChannel_DoesNotSendUserStatusOnline_WhenAlreadyInChannel()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _presenceServiceMock.Setup(p => p.IsUserInChannel("user1", "channel1"))
            .Returns(true);

        // Act
        await _hub.JoinChannel("channel1");

        // Assert
        _groupClientsMock.Verify(
            c => c.SendCoreAsync("UserStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- LeaveChannel tests ---

    [Fact]
    public async Task LeaveChannel_RemovesPresenceAndNotifiesGroup()
    {
        // Arrange
        SetupAuthenticatedUser("conn1", "user1");
        _contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("domain_user_id", Guid.NewGuid().ToString()),
            new Claim("display_name", "TestUser")
        }, "test")));

        // Act
        await _hub.LeaveChannel("channel1");

        // Assert
        _groupsMock.Verify(g => g.RemoveFromGroupAsync("conn1", "channel1", It.IsAny<CancellationToken>()), Times.Once);
        _presenceServiceMock.Verify(p => p.UserLeftChannelAsync("conn1", "channel1"), Times.Once);
    }
}
