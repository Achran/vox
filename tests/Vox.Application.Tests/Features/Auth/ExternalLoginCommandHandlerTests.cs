using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Application.Features.Auth.Commands.ExternalLogin;

namespace Vox.Application.Tests.Features.Auth;

public class ExternalLoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly ExternalLoginCommandHandler _handler;

    public ExternalLoginCommandHandlerTests()
    {
        _handler = new ExternalLoginCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingExternalLogin_ReturnsAuthTokensDto()
    {
        // Arrange
        var command = new ExternalLoginCommand("Google", "google-123", "test@example.com", "Test User");
        var expected = new AuthTokensDto(
            "access-token", "refresh-token", DateTime.UtcNow.AddHours(1),
            "user-id", "test@example.com", "testuser", "Test User");

        _identityServiceMock
            .Setup(s => s.ExternalLoginAsync(
                command.Provider, command.ProviderKey, command.Email, command.DisplayName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _identityServiceMock.Verify(
            s => s.ExternalLoginAsync(
                command.Provider, command.ProviderKey, command.Email, command.DisplayName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewExternalLogin_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var command = new ExternalLoginCommand("GitHub", "github-456", "dev@example.com", "Dev User");
        var expected = new AuthTokensDto(
            "new-access-token", "new-refresh-token", DateTime.UtcNow.AddHours(1),
            "new-user-id", "dev@example.com", "github_a1b2c3d4", "Dev User");

        _identityServiceMock
            .Setup(s => s.ExternalLoginAsync(
                command.Provider, command.ProviderKey, command.Email, command.DisplayName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithNullEmailAndDisplayName_DelegatesToService()
    {
        // Arrange
        var command = new ExternalLoginCommand("Steam", "steam-789", null, null);
        var expected = new AuthTokensDto(
            "steam-token", "steam-refresh", DateTime.UtcNow.AddHours(1),
            "steam-user-id", "steam_abcd1234@external.vox", "steam_abcd1234", "steam_abcd1234");

        _identityServiceMock
            .Setup(s => s.ExternalLoginAsync(
                "Steam", "steam-789", null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _identityServiceMock.Verify(
            s => s.ExternalLoginAsync("Steam", "steam-789", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var command = new ExternalLoginCommand("Google", "google-error", "test@example.com", "Test");

        _identityServiceMock
            .Setup(s => s.ExternalLoginAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User creation failed."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User creation failed.");
    }
}
