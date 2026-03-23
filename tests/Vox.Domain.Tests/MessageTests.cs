using Vox.Domain.Entities;

namespace Vox.Domain.Tests;

public class MessageTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsMessage()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var content = "Hello, world!";

        // Act
        var message = Message.Create(authorId, channelId, content);

        // Assert
        Assert.Equal(authorId, message.AuthorId);
        Assert.Equal(channelId, message.ChannelId);
        Assert.Equal(content, message.Content);
        Assert.False(message.IsEdited);
        Assert.False(message.IsDeleted);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Create_WithEmptyContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Message.Create(Guid.NewGuid(), Guid.NewGuid(), ""));
    }

    [Fact]
    public void Create_WithWhitespaceContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Message.Create(Guid.NewGuid(), Guid.NewGuid(), "   "));
    }

    [Fact]
    public void Edit_WithValidContent_UpdatesContentAndSetsEdited()
    {
        // Arrange
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");

        // Act
        message.Edit("Updated");

        // Assert
        Assert.Equal("Updated", message.Content);
        Assert.True(message.IsEdited);
    }

    [Fact]
    public void Edit_WithEmptyContent_ThrowsArgumentException()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");
        Assert.Throws<ArgumentException>(() => message.Edit(""));
    }

    [Fact]
    public void Delete_SetsIsDeleted()
    {
        // Arrange
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "To delete");

        // Act
        message.Delete();

        // Assert
        Assert.True(message.IsDeleted);
    }

    [Fact]
    public void Create_WithNullContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Message.Create(Guid.NewGuid(), Guid.NewGuid(), null!));
    }

    [Fact]
    public void Edit_WithNullContent_ThrowsArgumentException()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");
        Assert.Throws<ArgumentNullException>(() => message.Edit(null!));
    }

    [Fact]
    public void Edit_WithWhitespaceContent_ThrowsArgumentException()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");
        Assert.Throws<ArgumentException>(() => message.Edit("   "));
    }

    [Fact]
    public void Edit_DoesNotChangeIsDeleted()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");
        message.Edit("Updated");
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Delete_DoesNotChangeContent()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "Keep me");
        message.Delete();
        Assert.Equal("Keep me", message.Content);
    }
}
