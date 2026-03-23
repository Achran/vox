using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.Features.Auth.Commands.LinkProvider;

namespace Vox.Application.Tests.Features.Auth;

public class LinkProviderCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly LinkProviderCommandHandler _handler;

    public LinkProviderCommandHandlerTests()
    {
        _handler = new LinkProviderCommandHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_DelegatesToService()
    {
        // Arrange
        var command = new LinkProviderCommand("user-id", "Google", "google-123");

        _identityServiceMock
            .Setup(s => s.LinkExternalProviderAsync("user-id", "Google", "google-123",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(
            s => s.LinkExternalProviderAsync("user-id", "Google", "google-123",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProviderAlreadyLinked_PropagatesException()
    {
        // Arrange
        var command = new LinkProviderCommand("user-id", "Google", "google-123");

        _identityServiceMock
            .Setup(s => s.LinkExternalProviderAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A login for this provider already exists."));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A login for this provider already exists.");
    }
}
