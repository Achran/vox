using Vox.Domain.Entities;

namespace Vox.Domain.Tests;

public class ServerMemberTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serverId = Guid.NewGuid();

        // Act
        var member = ServerMember.Create(userId, serverId);

        // Assert
        Assert.Equal(userId, member.UserId);
        Assert.Equal(serverId, member.ServerId);
        Assert.Equal(ServerRole.Member, member.Role);
        Assert.NotEqual(Guid.Empty, member.Id);
    }

    [Fact]
    public void Create_WithOwnerRole_SetsRole()
    {
        // Arrange & Act
        var member = ServerMember.Create(Guid.NewGuid(), Guid.NewGuid(), ServerRole.Owner);

        // Assert
        Assert.Equal(ServerRole.Owner, member.Role);
    }

    [Fact]
    public void Create_WithAdminRole_SetsRole()
    {
        // Arrange & Act
        var member = ServerMember.Create(Guid.NewGuid(), Guid.NewGuid(), ServerRole.Admin);

        // Assert
        Assert.Equal(ServerRole.Admin, member.Role);
    }

    [Fact]
    public void Create_DefaultsToMemberRole()
    {
        // Arrange & Act
        var member = ServerMember.Create(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.Equal(ServerRole.Member, member.Role);
    }

    [Fact]
    public void UpdateRole_ChangesRole()
    {
        // Arrange
        var member = ServerMember.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act
        member.UpdateRole(ServerRole.Admin);

        // Assert
        Assert.Equal(ServerRole.Admin, member.Role);
    }

    [Fact]
    public void UpdateRole_UpdatesTimestamp()
    {
        // Arrange
        var member = ServerMember.Create(Guid.NewGuid(), Guid.NewGuid());
        var originalUpdatedAt = member.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure time difference
        member.UpdateRole(ServerRole.Admin);

        // Assert
        Assert.True(member.UpdatedAt >= originalUpdatedAt);
    }
}
