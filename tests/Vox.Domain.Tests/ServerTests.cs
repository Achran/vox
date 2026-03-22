using Vox.Domain.Entities;

namespace Vox.Domain.Tests;

public class ServerTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsServer()
    {
        // Arrange
        var name = "Test Server";
        var ownerId = Guid.NewGuid();

        // Act
        var server = Server.Create(name, ownerId);

        // Assert
        Assert.Equal(name, server.Name);
        Assert.Equal(ownerId, server.OwnerId);
        Assert.NotEqual(Guid.Empty, server.Id);
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        // Arrange & Act
        var server = Server.Create("Test", Guid.NewGuid(), "A test server");

        // Assert
        Assert.Equal("A test server", server.Description);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Server.Create("", Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Server.Create("   ", Guid.NewGuid()));
    }

    [Fact]
    public void Create_AddsDefaultTextChannel()
    {
        // Arrange & Act
        var server = Server.Create("Test", Guid.NewGuid());

        // Assert
        Assert.Contains(server.Channels, c => c.Name == "general" && c.Type == ChannelType.Text);
    }

    [Fact]
    public void Create_AddsDefaultVoiceChannel()
    {
        // Arrange & Act
        var server = Server.Create("Test", Guid.NewGuid());

        // Assert
        Assert.Contains(server.Channels, c => c.Name == "General" && c.Type == ChannelType.Voice);
    }

    [Fact]
    public void Create_AddsOwnerAsMember()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var server = Server.Create("Test", ownerId);

        // Assert
        Assert.Single(server.Members);
        Assert.Contains(server.Members, m => m.UserId == ownerId && m.Role == ServerRole.Owner);
    }

    [Fact]
    public void AddChannel_AddsChannelToServer()
    {
        // Arrange
        var server = Server.Create("Test", Guid.NewGuid());
        var initialCount = server.Channels.Count;

        // Act
        var channel = server.AddChannel("new-channel", ChannelType.Text);

        // Assert
        Assert.Equal(initialCount + 1, server.Channels.Count);
        Assert.Equal("new-channel", channel.Name);
        Assert.Equal(ChannelType.Text, channel.Type);
        Assert.Equal(server.Id, channel.ServerId);
    }

    [Fact]
    public void AddMember_AddsNewMember()
    {
        // Arrange
        var server = Server.Create("Test", Guid.NewGuid());
        var newUserId = Guid.NewGuid();

        // Act
        server.AddMember(newUserId);

        // Assert
        Assert.Equal(2, server.Members.Count);
        Assert.Contains(server.Members, m => m.UserId == newUserId && m.Role == ServerRole.Member);
    }

    [Fact]
    public void AddMember_DoesNotAddDuplicate()
    {
        // Arrange
        var server = Server.Create("Test", Guid.NewGuid());
        var newUserId = Guid.NewGuid();

        // Act
        server.AddMember(newUserId);
        server.AddMember(newUserId);

        // Assert
        Assert.Equal(2, server.Members.Count);
    }
}
