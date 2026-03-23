using FluentAssertions;
using Moq;
using Vox.Application.Features.Servers.Queries.GetServer;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Servers;

public class GetServerByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly GetServerByIdQueryHandler _handler;

    public GetServerByIdQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _handler = new GetServerByIdQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ServerExists_ReturnsServerDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test Server", ownerId, "A description");
        var query = new GetServerByIdQuery(server.Id);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Server");
        result.Description.Should().Be("A description");
        result.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_ServerNotFound_ReturnsNull()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var query = new GetServerByIdQuery(serverId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
