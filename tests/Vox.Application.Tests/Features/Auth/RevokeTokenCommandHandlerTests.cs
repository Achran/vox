using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.Features.Auth.Commands.RevokeToken;

namespace Vox.Application.Tests.Features.Auth;

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly RevokeTokenCommandHandler _handler;

    public RevokeTokenCommandHandlerTests()
    {
        _handler = new RevokeTokenCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_DelegatesToService()
    {
        // Arrange
        var command = new RevokeTokenCommand("valid-refresh-token");

        _identityServiceMock
            .Setup(s => s.RevokeAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(
            s => s.RevokeAsync(command.RefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_CompletesWithoutError()
    {
        // Revoke is silent if the token is not found
        var command = new RevokeTokenCommand("unknown-refresh-token");

        _identityServiceMock
            .Setup(s => s.RevokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _identityServiceMock.Verify(
            s => s.RevokeAsync(command.RefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
