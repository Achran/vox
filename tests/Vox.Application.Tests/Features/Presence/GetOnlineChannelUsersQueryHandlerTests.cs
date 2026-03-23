using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.Features.Presence.Queries.GetOnlineChannelUsers;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Presence;

public class GetOnlineChannelUsersQueryHandlerTests
{
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IChannelRepository> _channelRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetOnlineChannelUsersQueryHandler _handler;

    public GetOnlineChannelUsersQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Channels).Returns(_channelRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _handler = new GetOnlineChannelUsersQueryHandler(_presenceServiceMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsDisplayNames_WhenUsersExist()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());
        var query = new GetOnlineChannelUsersQuery(channelId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        var userId = Guid.NewGuid();
        var onlineIds = new List<string> { userId.ToString() };
        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForChannel(channelId.ToString()))
            .Returns(onlineIds);

        var user = User.Create("testuser", "test@test.com", "Test User");
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId.ToString());
        result[0].Status.Should().Be("Online");
        result[0].DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_ReturnsNullDisplayName_WhenUserNotFoundInDb()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());
        var query = new GetOnlineChannelUsersQuery(channelId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        var unknownUserId = Guid.NewGuid();
        var onlineIds = new List<string> { unknownUserId.ToString() };
        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForChannel(channelId.ToString()))
            .Returns(onlineIds);

        _userRepoMock.Setup(r => r.GetByIdAsync(unknownUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(unknownUserId.ToString());
        result[0].Status.Should().Be("Online");
        result[0].DisplayName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HandlesNonGuidUserIds_Gracefully()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());
        var query = new GetOnlineChannelUsersQuery(channelId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        var onlineIds = new List<string> { "not-a-guid" };
        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForChannel(channelId.ToString()))
            .Returns(onlineIds);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be("not-a-guid");
        result[0].Status.Should().Be("Online");
        result[0].DisplayName.Should().BeNull();
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenChannelNotFound()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var query = new GetOnlineChannelUsersQuery(channelId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Channel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoUsersOnline()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());
        var query = new GetOnlineChannelUsersQuery(channelId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForChannel(channelId.ToString()))
            .Returns(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
