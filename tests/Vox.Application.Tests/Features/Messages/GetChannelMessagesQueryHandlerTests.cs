using FluentAssertions;
using Moq;
using Vox.Application.Features.Messages.Queries.GetChannelMessages;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Messages;

public class GetChannelMessagesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMessageRepository> _messageRepoMock = new();
    private readonly GetChannelMessagesQueryHandler _handler;

    public GetChannelMessagesQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Messages).Returns(_messageRepoMock.Object);
        _handler = new GetChannelMessagesQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ChannelHasMessages_ReturnsMessageDtosInDescendingOrder()
    {
        // Arrange — repository returns messages in CreatedAt descending order (newest first)
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var olderMessage = Message.Create(authorId, channelId, "First message");
        var newerMessage = Message.Create(authorId, channelId, "Second message");

        var messages = new List<Message> { newerMessage, olderMessage };

        _messageRepoMock.Setup(r => r.GetByChannelIdAsync(channelId, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var query = new GetChannelMessagesQuery(channelId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — order matches repository contract (newest first)
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Second message");
        result[1].Content.Should().Be("First message");
    }

    [Fact]
    public async Task Handle_NoMessages_ReturnsEmptyList()
    {
        // Arrange
        var channelId = Guid.NewGuid();

        _messageRepoMock.Setup(r => r.GetByChannelIdAsync(channelId, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Message>());

        var query = new GetChannelMessagesQuery(channelId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PageSizeExceeds100_ClampedTo100()
    {
        // Arrange
        var channelId = Guid.NewGuid();

        _messageRepoMock.Setup(r => r.GetByChannelIdAsync(channelId, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Message>());

        var query = new GetChannelMessagesQuery(channelId, PageSize: 200);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _messageRepoMock.Verify(r => r.GetByChannelIdAsync(channelId, 100, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBeforeParameter_PassesItToRepository()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var before = DateTime.UtcNow.AddHours(-1);

        _messageRepoMock.Setup(r => r.GetByChannelIdAsync(channelId, 50, before, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Message>());

        var query = new GetChannelMessagesQuery(channelId, Before: before);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _messageRepoMock.Verify(r => r.GetByChannelIdAsync(channelId, 50, before, It.IsAny<CancellationToken>()), Times.Once);
    }
}
