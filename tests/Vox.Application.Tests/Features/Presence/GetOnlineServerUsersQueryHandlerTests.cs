using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.Features.Presence.Queries.GetOnlineServerUsers;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Presence;

public class GetOnlineServerUsersQueryHandlerTests
{
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetOnlineServerUsersQueryHandler _handler;

    public GetOnlineServerUsersQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _handler = new GetOnlineServerUsersQueryHandler(_presenceServiceMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsDisplayNames_WhenUsersExist()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var query = new GetOnlineServerUsersQuery(server.Id);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        var onlineIds = new List<string> { ownerId.ToString() };
        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForServer(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(onlineIds);

        var user = User.Create("testuser", "test@test.com", "Test User");
        _userRepoMock.Setup(r => r.GetByIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(ownerId.ToString());
        result[0].Status.Should().Be("Online");
        result[0].DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_ReturnsNullDisplayName_WhenUserNotFoundInDb()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var query = new GetOnlineServerUsersQuery(server.Id);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        var unknownUserId = Guid.NewGuid();
        var onlineIds = new List<string> { unknownUserId.ToString() };
        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForServer(It.IsAny<IReadOnlyList<Guid>>()))
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
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var query = new GetOnlineServerUsersQuery(server.Id);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        var onlineIds = new List<string> { "not-a-guid" };
        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForServer(It.IsAny<IReadOnlyList<Guid>>()))
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
    public async Task Handle_ThrowsKeyNotFoundException_WhenServerNotFound()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var query = new GetOnlineServerUsersQuery(serverId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoUsersOnline()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var query = new GetOnlineServerUsersQuery(server.Id);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        _presenceServiceMock.Setup(p => p.GetOnlineUserIdsForServer(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
