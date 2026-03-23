using Vox.Domain.Entities;

namespace Vox.Domain.Tests;

public class ChannelTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsChannel()
    {
        // Arrange
        var name = "general";
        var type = ChannelType.Text;
        var serverId = Guid.NewGuid();

        // Act
        var channel = Channel.Create(name, type, serverId);

        // Assert
        Assert.Equal(name, channel.Name);
        Assert.Equal(type, channel.Type);
        Assert.Equal(serverId, channel.ServerId);
        Assert.NotEqual(Guid.Empty, channel.Id);
    }

    [Fact]
    public void Create_WithVoiceType_SetsType()
    {
        // Arrange & Act
        var channel = Channel.Create("voice", ChannelType.Voice, Guid.NewGuid());

        // Assert
        Assert.Equal(ChannelType.Voice, channel.Type);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Channel.Create("", ChannelType.Text, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Channel.Create("   ", ChannelType.Text, Guid.NewGuid()));
    }

    [Fact]
    public void PostMessage_AddsMessageToChannel()
    {
        // Arrange
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());
        var authorId = Guid.NewGuid();

        // Act
        var message = channel.PostMessage(authorId, "Hello!");

        // Assert
        Assert.Single(channel.Messages);
        Assert.Equal("Hello!", message.Content);
        Assert.Equal(authorId, message.AuthorId);
        Assert.Equal(channel.Id, message.ChannelId);
    }

    [Fact]
    public void PostMessage_MultipleTimes_AddsAllMessages()
    {
        // Arrange
        var channel = Channel.Create("general", ChannelType.Text, Guid.NewGuid());
        var authorId = Guid.NewGuid();

        // Act
        channel.PostMessage(authorId, "First");
        channel.PostMessage(authorId, "Second");

        // Assert
        Assert.Equal(2, channel.Messages.Count);
    }

    [Fact]
    public void UpdateName_ChangesChannelName()
    {
        // Arrange
        var channel = Channel.Create("old-name", ChannelType.Text, Guid.NewGuid());

        // Act
        channel.UpdateName("new-name");

        // Assert
        Assert.Equal("new-name", channel.Name);
    }

    [Fact]
    public void UpdateName_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var channel = Channel.Create("test", ChannelType.Text, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => channel.UpdateName(""));
    }
}
