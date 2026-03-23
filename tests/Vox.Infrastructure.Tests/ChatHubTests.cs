using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Application.Features.Messages.Commands.SendMessage;
using Vox.Infrastructure.Hubs;

namespace Vox.Infrastructure.Tests;

public class ChatHubTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<IHubCallerClients> _clientsMock = new();
    private readonly Mock<IGroupManager> _groupsMock = new();
    private readonly Mock<HubCallerContext> _contextMock = new();
    private readonly Mock<IClientProxy> _groupClientProxyMock = new();
    private readonly Mock<IClientProxy> _othersProxyMock = new();
    private readonly ChatHub _hub;

    public ChatHubTests()
    {
        _hub = new ChatHub(_mediatorMock.Object, _presenceServiceMock.Object)
        {
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object,
            Context = _contextMock.Object
        };
    }

    private void SetupAuthenticatedUser(Guid domainUserId)
    {
        var claims = new[]
        {
            new Claim("domain_user_id", domainUserId.ToString()),
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("display_name", "Test User"),
            new Claim("unique_name", "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _contextMock.Setup(c => c.User).Returns(principal);
        _contextMock.Setup(c => c.UserIdentifier).Returns(domainUserId.ToString());
        _contextMock.Setup(c => c.ConnectionId).Returns(Guid.NewGuid().ToString());
    }

    // -------------------------------------------------------------------------
    // SendMessage
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendMessage_WithValidData_BroadcastsToGroup()
    {
        // Arrange
        var domainUserId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        SetupAuthenticatedUser(domainUserId);

        var messageDto = new MessageDto(
            Guid.NewGuid(), domainUserId, channelId, "Hello!", false, DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageDto);

        _clientsMock.Setup(c => c.Group(channelId.ToString())).Returns(_groupClientProxyMock.Object);

        // Act
        await _hub.SendMessage(channelId.ToString(), "Hello!");

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SendMessageCommand>(cmd =>
                cmd.ChannelId == channelId &&
                cmd.Content == "Hello!" &&
                cmd.AuthorId == domainUserId),
            It.IsAny<CancellationToken>()), Times.Once);

        _groupClientProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithInvalidChannelId_ThrowsHubException()
    {
        // Arrange
        var domainUserId = Guid.NewGuid();
        SetupAuthenticatedUser(domainUserId);

        // Act & Assert
        var act = () => _hub.SendMessage("not-a-guid", "Hello!");
        await act.Should().ThrowAsync<HubException>().WithMessage("Invalid channel ID.");
    }

    [Fact]
    public async Task SendMessage_WithoutAuthentication_ThrowsHubException()
    {
        // Arrange - no user claims set up
        _contextMock.Setup(c => c.User).Returns((ClaimsPrincipal?)null);

        // Act & Assert
        var act = () => _hub.SendMessage(Guid.NewGuid().ToString(), "Hello!");
        await act.Should().ThrowAsync<HubException>().WithMessage("Unauthorized.");
    }

    // -------------------------------------------------------------------------
    // JoinChannel / LeaveChannel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinChannel_AddsToGroupAndNotifiesChannel()
    {
        // Arrange
        var domainUserId = Guid.NewGuid();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(domainUserId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _presenceServiceMock.Setup(p => p.IsUserInChannel(domainUserId.ToString(), channelId)).Returns(false);
        _clientsMock.Setup(c => c.Group(channelId)).Returns(_groupClientProxyMock.Object);

        // Act
        await _hub.JoinChannel(channelId);

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync(connectionId, channelId, It.IsAny<CancellationToken>()), Times.Once);
        _presenceServiceMock.Verify(p => p.UserJoinedChannelAsync(connectionId, channelId), Times.Once);
        _groupClientProxyMock.Verify(
            c => c.SendCoreAsync("UserJoined", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveChannel_RemovesFromGroupAndNotifiesChannel()
    {
        // Arrange
        var domainUserId = Guid.NewGuid();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(domainUserId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _clientsMock.Setup(c => c.Group(channelId)).Returns(_groupClientProxyMock.Object);

        // Act
        await _hub.LeaveChannel(channelId);

        // Assert
        _groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, channelId, It.IsAny<CancellationToken>()), Times.Once);
        _presenceServiceMock.Verify(p => p.UserLeftChannelAsync(connectionId, channelId), Times.Once);
    }

    // -------------------------------------------------------------------------
    // StartTyping
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartTyping_SendsTypingNotificationToOthersInGroup()
    {
        // Arrange
        var domainUserId = Guid.NewGuid();
        var channelId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(domainUserId);

        _clientsMock.Setup(c => c.OthersInGroup(channelId)).Returns(_othersProxyMock.Object);

        // Act
        await _hub.StartTyping(channelId);

        // Assert
        _othersProxyMock.Verify(
            c => c.SendCoreAsync("UserTyping", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // JoinServer / LeaveServer
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinServer_AddsToServerGroup()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.JoinServer(serverId);

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync(connectionId, $"server:{serverId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveServer_RemovesFromServerGroup()
    {
        // Arrange
        var serverId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.LeaveServer(serverId);

        // Assert
        _groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, $"server:{serverId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Heartbeat
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Heartbeat_CallsPresenceService()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.Heartbeat();

        // Assert
        _presenceServiceMock.Verify(p => p.HeartbeatAsync(connectionId), Times.Once);
    }
}
