using FluentAssertions;
using Moq;
using Vox.Application.Features.Messages.Commands.SendMessage;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Messages;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IChannelRepository> _channelRepoMock = new();
    private readonly Mock<IMessageRepository> _messageRepoMock = new();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Channels).Returns(_channelRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Messages).Returns(_messageRepoMock.Object);
        _handler = new SendMessageCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsMessageDto()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        var command = new SendMessageCommand(channelId, "Hello!", authorId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Content.Should().Be("Hello!");
        result.AuthorId.Should().Be(authorId);
        result.ChannelId.Should().Be(channelId);
        result.IsEdited.Should().BeFalse();
        result.Id.Should().NotBeEmpty();

        _messageRepoMock.Verify(r => r.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ChannelNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Channel?)null);

        var command = new SendMessageCommand(channelId, "Hello!", Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
