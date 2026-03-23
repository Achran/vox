using FluentAssertions;
using Moq;
using Vox.Application.Features.Channels.Commands.UpdateChannel;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Channels;

public class UpdateChannelCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly Mock<IChannelRepository> _channelRepoMock = new();
    private readonly UpdateChannelCommandHandler _handler;

    public UpdateChannelCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Channels).Returns(_channelRepoMock.Object);
        _handler = new UpdateChannelCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_AsOwner_UpdatesChannelAndReturnsDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var channel = Channel.Create("old-name", ChannelType.Text, server.Id);
        var command = new UpdateChannelCommand(channel.Id, "new-name", ownerId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);
        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("new-name");
        result.Type.Should().Be("Text");
        _channelRepoMock.Verify(r => r.UpdateAsync(channel, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AsNonOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var channel = Channel.Create("test", ChannelType.Text, server.Id);
        var command = new UpdateChannelCommand(channel.Id, "new-name", otherUserId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);
        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only the server owner can update channels.");
    }

    [Fact]
    public async Task Handle_ChannelNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var command = new UpdateChannelCommand(channelId, "new-name", Guid.NewGuid());

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Channel?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
