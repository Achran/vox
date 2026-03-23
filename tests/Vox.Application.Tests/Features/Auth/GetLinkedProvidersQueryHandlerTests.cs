using FluentAssertions;
using Moq;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Application.Features.Auth.Queries.GetLinkedProviders;

namespace Vox.Application.Tests.Features.Auth;

public class GetLinkedProvidersQueryHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly GetLinkedProvidersQueryHandler _handler;

    public GetLinkedProvidersQueryHandlerTests()
    {
        _handler = new GetLinkedProvidersQueryHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsLinkedProviders()
    {
        // Arrange
        var query = new GetLinkedProvidersQuery("user-id");
        var expected = new List<LinkedAccountDto>
        {
            new("Google", "google-123", "Google"),
            new("GitHub", "github-456", "GitHub")
        };

        _identityServiceMock
            .Setup(s => s.GetLinkedProvidersAsync("user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _identityServiceMock.Verify(
            s => s.GetLinkedProvidersAsync("user-id", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoLinkedProviders_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetLinkedProvidersQuery("user-id");
        var expected = new List<LinkedAccountDto>();

        _identityServiceMock
            .Setup(s => s.GetLinkedProvidersAsync("user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var query = new GetLinkedProvidersQuery("invalid-user");

        _identityServiceMock
            .Setup(s => s.GetLinkedProvidersAsync("invalid-user", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User not found."));

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User not found.");
    }
}
