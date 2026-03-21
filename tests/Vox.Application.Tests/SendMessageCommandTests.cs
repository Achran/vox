using Moq;
using Vox.Application.Features.Messages.Commands;
using Vox.Domain.Entities;
using Vox.Domain.Repositories;

namespace Vox.Application.Tests;

public class SendMessageCommandTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnMessageDto()
    {
        var authorId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        var mockMessageRepo = new Mock<IMessageRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        mockMessageRepo
            .Setup(r => r.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SendMessageCommandHandler(mockMessageRepo.Object, mockUnitOfWork.Object);
        var command = new SendMessageCommand("Hello World", authorId, channelId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Hello World", result.Content);
        Assert.Equal(authorId, result.AuthorId);
        Assert.Equal(channelId, result.ChannelId);
    }

    [Fact]
    public void Validator_WithEmptyContent_ShouldFail()
    {
        var validator = new SendMessageCommandValidator();
        var command = new SendMessageCommand("", Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }
}
