using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.Features.Auth.Commands.UnlinkProvider;

namespace Vox.Application.Tests.Features.Auth;

public class UnlinkProviderCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly UnlinkProviderCommandHandler _handler;

    public UnlinkProviderCommandHandlerTests()
    {
        _handler = new UnlinkProviderCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_DelegatesToService()
    {
        // Arrange
        var command = new UnlinkProviderCommand("user-id", "Google");

        _identityServiceMock
            .Setup(s => s.UnlinkExternalProviderAsync("user-id", "Google",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(
            s => s.UnlinkExternalProviderAsync("user-id", "Google",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLastLoginMethod_PropagatesException()
    {
        // Arrange
        var command = new UnlinkProviderCommand("user-id", "Google");

        _identityServiceMock
            .Setup(s => s.UnlinkExternalProviderAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot unlink the last login method."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot unlink the last login method.");
    }

    [Fact]
    public async Task Handle_WhenProviderNotLinked_PropagatesException()
    {
        // Arrange
        var command = new UnlinkProviderCommand("user-id", "Facebook");

        _identityServiceMock
            .Setup(s => s.UnlinkExternalProviderAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider 'Facebook' is not linked to this account."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Provider 'Facebook' is not linked to this account.");
    }
}
