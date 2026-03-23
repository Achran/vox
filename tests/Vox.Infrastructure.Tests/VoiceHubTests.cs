using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Vox.Application.Abstractions;
using Vox.Infrastructure.Hubs;

namespace Vox.Infrastructure.Tests;

public class VoiceHubTests
{
    private readonly Mock<IVoiceSessionService> _voiceSessionMock = new();
    private readonly Mock<IHubCallerClients> _clientsMock = new();
    private readonly Mock<IGroupManager> _groupsMock = new();
    private readonly Mock<HubCallerContext> _contextMock = new();
    private readonly Mock<ISingleClientProxy> _callerProxyMock = new();
    private readonly Mock<IClientProxy> _groupProxyMock = new();
    private readonly Mock<IClientProxy> _othersInGroupProxyMock = new();
    private readonly Mock<IClientProxy> _userProxyMock = new();
    private readonly VoiceHub _hub;

    public VoiceHubTests()
    {
        _hub = new VoiceHub(_voiceSessionMock.Object)
        {
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object,
            Context = _contextMock.Object
        };
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new[]
        {
            new Claim("domain_user_id", userId),
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("display_name", "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _contextMock.Setup(c => c.User).Returns(principal);
        _contextMock.Setup(c => c.UserIdentifier).Returns(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(Guid.NewGuid().ToString());
    }

    // -------------------------------------------------------------------------
    // JoinVoiceChannel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinVoiceChannel_NewParticipant_AddsToGroupFirstThenNotifies()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.JoinChannel(channelId, userId, connectionId)).Returns(true);
        _voiceSessionMock.Setup(v => v.GetParticipants(channelId)).Returns(new List<string> { userId });

        _clientsMock.Setup(c => c.OthersInGroup($"voice:{channelId}")).Returns(_othersInGroupProxyMock.Object);
        _clientsMock.Setup(c => c.Caller).Returns(_callerProxyMock.Object);

        // Act
        await _hub.JoinVoiceChannel(channelId);

        // Assert – group add happens first
        _groupsMock.Verify(g => g.AddToGroupAsync(connectionId, $"voice:{channelId}", It.IsAny<CancellationToken>()), Times.Once);
        _voiceSessionMock.Verify(v => v.JoinChannel(channelId, userId, connectionId), Times.Once);
        _othersInGroupProxyMock.Verify(
            c => c.SendCoreAsync("UserJoinedVoice", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _callerProxyMock.Verify(
            c => c.SendCoreAsync("VoiceParticipants", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinVoiceChannel_ExistingParticipant_DoesNotNotifyOthers()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.JoinChannel(channelId, userId, connectionId)).Returns(false);
        _voiceSessionMock.Setup(v => v.GetParticipants(channelId)).Returns(new List<string> { userId });

        _clientsMock.Setup(c => c.Caller).Returns(_callerProxyMock.Object);

        // Act
        await _hub.JoinVoiceChannel(channelId);

        // Assert
        _othersInGroupProxyMock.Verify(
            c => c.SendCoreAsync("UserJoinedVoice", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task JoinVoiceChannel_SessionFailure_RollsBackGroupMembership()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.JoinChannel(channelId, userId, connectionId))
            .Throws(new InvalidOperationException("Session failure"));

        // Act & Assert
        var act = () => _hub.JoinVoiceChannel(channelId);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Group add was called, then rollback removed it
        _groupsMock.Verify(g => g.AddToGroupAsync(connectionId, $"voice:{channelId}", It.IsAny<CancellationToken>()), Times.Once);
        _groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, $"voice:{channelId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // LeaveVoiceChannel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LeaveVoiceChannel_LastConnection_NotifiesGroup()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.LeaveChannel(channelId, userId, connectionId)).Returns(true);
        _clientsMock.Setup(c => c.Group($"voice:{channelId}")).Returns(_groupProxyMock.Object);

        // Act
        await _hub.LeaveVoiceChannel(channelId);

        // Assert
        _groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, $"voice:{channelId}", It.IsAny<CancellationToken>()), Times.Once);
        _groupProxyMock.Verify(
            c => c.SendCoreAsync("UserLeftVoice", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveVoiceChannel_NotLastConnection_DoesNotNotifyGroup()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.LeaveChannel(channelId, userId, connectionId)).Returns(false);

        // Act
        await _hub.LeaveVoiceChannel(channelId);

        // Assert
        _groupProxyMock.Verify(
            c => c.SendCoreAsync("UserLeftVoice", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // SendOffer / SendAnswer / SendIceCandidate – with membership validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendOffer_BothInChannel_RelaysToTargetUser()
    {
        // Arrange
        var senderId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(senderId);

        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, senderId)).Returns(true);
        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, targetId)).Returns(true);
        _clientsMock.Setup(c => c.User(targetId)).Returns(_userProxyMock.Object);

        // Act
        await _hub.SendOffer(targetId, channelId, "sdp-offer-data");

        // Assert
        _userProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveOffer",
                It.Is<object?[]>(args => args.Length == 3 && (string)args[0]! == senderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendOffer_SenderNotInChannel_ThrowsHubException()
    {
        // Arrange
        var senderId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(senderId);

        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, senderId)).Returns(false);

        // Act & Assert
        var act = () => _hub.SendOffer(targetId, channelId, "sdp");
        await act.Should().ThrowAsync<HubException>().WithMessage("You are not in this voice channel.");
    }

    [Fact]
    public async Task SendOffer_TargetNotInChannel_ThrowsHubException()
    {
        // Arrange
        var senderId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(senderId);

        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, senderId)).Returns(true);
        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, targetId)).Returns(false);

        // Act & Assert
        var act = () => _hub.SendOffer(targetId, channelId, "sdp");
        await act.Should().ThrowAsync<HubException>().WithMessage("Target user is not in this voice channel.");
    }

    [Fact]
    public async Task SendAnswer_BothInChannel_RelaysToTargetUser()
    {
        // Arrange
        var senderId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(senderId);

        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, senderId)).Returns(true);
        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, targetId)).Returns(true);
        _clientsMock.Setup(c => c.User(targetId)).Returns(_userProxyMock.Object);

        // Act
        await _hub.SendAnswer(targetId, channelId, "sdp-answer-data");

        // Assert
        _userProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveAnswer",
                It.Is<object?[]>(args => args.Length == 3 && (string)args[0]! == senderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendIceCandidate_BothInChannel_RelaysToTargetUser()
    {
        // Arrange
        var senderId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(senderId);

        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, senderId)).Returns(true);
        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, targetId)).Returns(true);
        _clientsMock.Setup(c => c.User(targetId)).Returns(_userProxyMock.Object);

        // Act
        await _hub.SendIceCandidate(targetId, channelId, "ice-candidate-data");

        // Assert
        _userProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveIceCandidate",
                It.Is<object?[]>(args => args.Length == 3 && (string)args[0]! == senderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // OnDisconnectedAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnectionAndNotifiesChannels()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.RemoveConnection(connectionId))
            .Returns(new List<string> { channelId });
        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, userId)).Returns(false);

        _clientsMock.Setup(c => c.Group($"voice:{channelId}")).Returns(_groupProxyMock.Object);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _voiceSessionMock.Verify(v => v.RemoveConnection(connectionId), Times.Once);
        _groupProxyMock.Verify(
            c => c.SendCoreAsync("UserLeftVoice", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_UserStillConnected_DoesNotNotify()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var channelId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        SetupAuthenticatedUser(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        _voiceSessionMock.Setup(v => v.RemoveConnection(connectionId))
            .Returns(new List<string> { channelId });
        _voiceSessionMock.Setup(v => v.IsUserInVoiceChannel(channelId, userId)).Returns(true);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _groupProxyMock.Verify(
            c => c.SendCoreAsync("UserLeftVoice", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // Unauthorized access
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinVoiceChannel_WithoutAuth_ThrowsHubException()
    {
        // Arrange - no user set up
        _contextMock.Setup(c => c.UserIdentifier).Returns((string?)null);

        // Act & Assert
        var act = () => _hub.JoinVoiceChannel(Guid.NewGuid().ToString());
        await act.Should().ThrowAsync<HubException>().WithMessage("Unauthorized.");
    }

    [Fact]
    public async Task SendOffer_WithoutAuth_ThrowsHubException()
    {
        // Arrange
        _contextMock.Setup(c => c.UserIdentifier).Returns((string?)null);

        // Act & Assert
        var act = () => _hub.SendOffer("target", "channel", "sdp");
        await act.Should().ThrowAsync<HubException>().WithMessage("Unauthorized.");
    }
}
