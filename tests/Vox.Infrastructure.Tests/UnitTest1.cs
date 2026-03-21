using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Infrastructure.Persistence;
using Vox.Infrastructure.Persistence.Repositories;

namespace Vox.Infrastructure.Tests;

public class UserRepositoryTests
{
    private static VoxDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<VoxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new VoxDbContext(options);
    }

    [Fact]
    public async Task AddAsync_AddsUserToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new UserRepository(context);
        var user = User.Create("testuser", "test@example.com", "Test User");

        // Act
        await repo.AddAsync(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Set<User>().FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("testuser", savedUser.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new UserRepository(context);
        var user = User.Create("testuser", "test@example.com", "Test User");
        await repo.AddAsync(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ReturnsTrue_WhenEmailExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new UserRepository(context);
        var user = User.Create("testuser", "test@example.com", "Test User");
        await repo.AddAsync(user);
        await context.SaveChangesAsync();

        // Act
        var exists = await repo.ExistsByEmailAsync("test@example.com");

        // Assert
        Assert.True(exists);
    }
}
