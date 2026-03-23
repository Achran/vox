using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Application.Features.Auth.Commands.RefreshToken;

namespace Vox.Application.Tests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_ReturnsNewAuthTokensDto()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-refresh-token");
        var expected = new AuthTokensDto(
            "new-access-token", "new-refresh-token", DateTime.UtcNow.AddHours(1),
            "user-id", "test@example.com", "testuser", "Test User");

        _identityServiceMock
            .Setup(s => s.RefreshAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _identityServiceMock.Verify(
            s => s.RefreshAsync(command.RefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpiredRefreshToken_PropagatesException()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired-refresh-token");

        _identityServiceMock
            .Setup(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Refresh token is expired or revoked."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token is expired or revoked.");
    }

    [Fact]
    public async Task Handle_WithRevokedRefreshToken_PropagatesException()
    {
        // Arrange
        var command = new RefreshTokenCommand("revoked-refresh-token");

        _identityServiceMock
            .Setup(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
