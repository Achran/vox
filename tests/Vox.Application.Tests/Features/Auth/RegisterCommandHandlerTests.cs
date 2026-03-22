using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Application.Features.Auth.Commands.Register;

namespace Vox.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsAuthTokensDto()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "test@example.com", "Test User", "Password1");
        var expected = new AuthTokensDto(
            "access-token", "refresh-token", DateTime.UtcNow.AddHours(1),
            "user-id", "test@example.com", "testuser", "Test User");

        _identityServiceMock
            .Setup(s => s.RegisterAsync(command.UserName, command.Email, command.DisplayName, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _identityServiceMock.Verify(
            s => s.RegisterAsync(command.UserName, command.Email, command.DisplayName, command.Password, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRegistrationFails_PropagatesException()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "test@example.com", "Test User", "Password1");

        _identityServiceMock
            .Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email already taken."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Email already taken.");
    }
}
