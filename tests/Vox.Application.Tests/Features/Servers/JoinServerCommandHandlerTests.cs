using FluentAssertions;
using Moq;
using Vox.Application.Features.Servers.Commands.JoinServer;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Servers;

public class JoinServerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IServerRepository> _serverRepoMock = new();
    private readonly Mock<IServerMemberRepository> _memberRepoMock = new();
    private readonly JoinServerCommandHandler _handler;

    public JoinServerCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Servers).Returns(_serverRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ServerMembers).Returns(_memberRepoMock.Object);
        _handler = new JoinServerCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_JoinsServerAndReturnsDto()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var server = Server.Create("Test", ownerId);
        var command = new JoinServerCommand(server.Id, newUserId);

        _serverRepoMock.Setup(r => r.GetByIdAsync(server.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(server);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Test");
        result.OwnerId.Should().Be(ownerId);
        _memberRepoMock.Verify(r => r.AddAsync(It.Is<ServerMember>(m => m.UserId == newUserId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ServerNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var command = new JoinServerCommand(serverId, Guid.NewGuid());

        _serverRepoMock.Setup(r => r.GetByIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
