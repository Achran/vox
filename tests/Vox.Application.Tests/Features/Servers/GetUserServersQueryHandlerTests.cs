using FluentAssertions;
using Moq;
using Vox.Application.Features.Servers.Queries.GetUserServers;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Servers;

public class GetUserServersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly GetUserServersQueryHandler _handler;

    public GetUserServersQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _handler = new GetUserServersQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_UserHasServers_ReturnsServerDtos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var servers = new List<Server>
        {
            Server.Create("Server 1", userId),
            Server.Create("Server 2", userId)
        };
        var query = new GetUserServersQuery(userId);

        _serverRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servers);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Server 1");
        result[1].Name.Should().Be("Server 2");
    }

    [Fact]
    public async Task Handle_UserHasNoServers_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserServersQuery(userId);

        _serverRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Server>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
