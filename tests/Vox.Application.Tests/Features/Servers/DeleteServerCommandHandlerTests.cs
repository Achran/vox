using FluentAssertions;
using Moq;
using Vox.Application.Features.Servers.Commands.DeleteServer;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Servers;

public class DeleteServerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly DeleteServerCommandHandler _handler;

    public DeleteServerCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _handler = new DeleteServerCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_AsOwner_DeletesServer()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var command = new DeleteServerCommand(server.Id, ownerId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _serverRepoMock.Verify(r => r.DeleteAsync(server.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AsNonOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var command = new DeleteServerCommand(server.Id, otherUserId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only the server owner can delete the server.");
    }

    [Fact]
    public async Task Handle_ServerNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var command = new DeleteServerCommand(serverId, Guid.NewGuid());

        _serverRepoMock.Setup(r => r.GetByIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
