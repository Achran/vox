using Vox.Domain.Entities;

namespace Vox.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsUser()
    {
        // Arrange
        var userName = "testuser";
        var email = "test@example.com";
        var displayName = "Test User";

        // Act
        var user = User.Create(userName, email, displayName);

        // Assert
        Assert.Equal(userName, user.UserName);
        Assert.Equal(email, user.Email);
        Assert.Equal(displayName, user.DisplayName);
        Assert.Equal(UserStatus.Offline, user.Status);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Create_RaisesUserCreatedEvent()
    {
        // Arrange & Act
        var user = User.Create("testuser", "test@example.com", "Test User");

        // Assert
        Assert.Single(user.DomainEvents);
    }

    [Fact]
    public void Create_WithEmptyUserName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => User.Create("", "test@example.com", "Test User"));
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "Test User");

        // Act
        user.UpdateStatus(UserStatus.Online);

        // Assert
        Assert.Equal(UserStatus.Online, user.Status);
    }
}
