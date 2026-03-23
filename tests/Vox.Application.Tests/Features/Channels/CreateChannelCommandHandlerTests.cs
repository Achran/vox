using FluentAssertions;
using Moq;
using Vox.Application.Features.Channels.Commands.CreateChannel;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Channels;

public class CreateChannelCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly CreateChannelCommandHandler _handler;

    public CreateChannelCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _handler = new CreateChannelCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_AsOwner_CreatesChannelAndReturnsDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var command = new CreateChannelCommand(server.Id, "new-channel", "Text", ownerId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("new-channel");
        result.Type.Should().Be("Text");
        result.ServerId.Should().Be(server.Id);
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
        var command = new CreateChannelCommand(server.Id, "new-channel", "Text", otherUserId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only the server owner can create channels.");
    }

    [Fact]
    public async Task Handle_InvalidChannelType_ThrowsInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var command = new CreateChannelCommand(server.Id, "new-channel", "InvalidType", ownerId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid channel type*");
    }

    [Fact]
    public async Task Handle_ServerNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var command = new CreateChannelCommand(serverId, "new-channel", "Text", Guid.NewGuid());

        _serverRepoMock.Setup(r => r.GetByIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
