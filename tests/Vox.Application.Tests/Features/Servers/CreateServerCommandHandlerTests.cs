using FluentAssertions;
using Moq;
using Vox.Application.Features.Servers.Commands.CreateServer;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Servers;

public class CreateServerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly CreateServerCommandHandler _handler;

    public CreateServerCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _handler = new CreateServerCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsServerDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var command = new CreateServerCommand("My Server", "A test server", ownerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("My Server");
        result.Description.Should().Be("A test server");
        result.OwnerId.Should().Be(ownerId);
        result.Id.Should().NotBeEmpty();

        _serverRepoMock.Verify(r => r.AddAsync(It.IsAny<Server>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateServerCommand("", null, Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
