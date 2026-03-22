using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Application.Features.Auth.Commands.Login;

namespace Vox.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthTokensDto()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password1");
        var expected = new AuthTokensDto(
            "access-token", "refresh-token", DateTime.UtcNow.AddHours(1),
            "user-id", "test@example.com", "testuser", "Test User");

        _identityServiceMock
            .Setup(s => s.LoginAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _identityServiceMock.Verify(
            s => s.LoginAsync(command.Email, command.Password, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_PropagatesException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword");

        _identityServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid email or password.");
    }
}
