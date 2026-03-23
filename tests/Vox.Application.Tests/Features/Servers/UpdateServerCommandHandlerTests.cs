using FluentAssertions;
using Moq;
using Vox.Application.Features.Servers.Commands.UpdateServer;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Servers;

public class UpdateServerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly UpdateServerCommandHandler _handler;

    public UpdateServerCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _handler = new UpdateServerCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_AsOwner_UpdatesServerAndReturnsDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Old Name", ownerId, "Old Description");
        var command = new UpdateServerCommand(server.Id, "New Name", "New Description", ownerId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Description");
        _serverRepoMock.Verify(r => r.UpdateAsync(server, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AsNonOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var command = new UpdateServerCommand(server.Id, "New Name", null, otherUserId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only the server owner can update the server.");
    }

    [Fact]
    public async Task Handle_ServerNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var command = new UpdateServerCommand(serverId, "New Name", null, Guid.NewGuid());

        _serverRepoMock.Setup(r => r.GetByIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
