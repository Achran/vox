using Vox.Domain.Entities;

namespace Vox.Domain.Tests;

public class UserEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        var user = User.Create("john", "john@example.com", "John Doe");

        Assert.Equal("john", user.Username);
        Assert.Equal("john@example.com", user.Email);
        Assert.Equal("John Doe", user.DisplayName);
        Assert.Equal(UserStatus.Offline, user.Status);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Create_WithEmptyUsername_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => User.Create("", "john@example.com", "John Doe"));
    }
}

public class ServerEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateServer()
    {
        var ownerId = Guid.NewGuid();
        var server = Server.Create("My Server", ownerId);

        Assert.Equal("My Server", server.Name);
        Assert.Equal(ownerId, server.OwnerId);
        Assert.Empty(server.Channels);
    }

    [Fact]
    public void AddChannel_ShouldAddChannelToServer()
    {
        var server = Server.Create("My Server", Guid.NewGuid());
        server.AddChannel("general");

        Assert.Single(server.Channels);
        Assert.Equal("general", server.Channels.First().Name);
    }
}

public class MessageEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateMessage()
    {
        var authorId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var message = Message.Create("Hello World", authorId, channelId);

        Assert.Equal("Hello World", message.Content);
        Assert.Equal(authorId, message.AuthorId);
        Assert.False(message.IsEdited);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Edit_ShouldUpdateContentAndMarkAsEdited()
    {
        var message = Message.Create("Original", Guid.NewGuid(), Guid.NewGuid());
        message.Edit("Updated content");

        Assert.Equal("Updated content", message.Content);
        Assert.True(message.IsEdited);
    }
}
