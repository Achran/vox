using FluentAssertions;
using Moq;
using Vox.Application.Features.Channels.Queries.GetChannel;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Channels;

public class GetChannelByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IChannelRepository> _channelRepoMock = new();
    private readonly GetChannelByIdQueryHandler _handler;

    public GetChannelByIdQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Channels).Returns(_channelRepoMock.Object);
        _handler = new GetChannelByIdQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ChannelExists_ReturnsChannelDto()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var channel = Channel.Create("test-channel", ChannelType.Text, serverId);
        var query = new GetChannelByIdQuery(channel.Id);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-channel");
        result.Type.Should().Be("Text");
        result.ServerId.Should().Be(serverId);
    }

    [Fact]
    public async Task Handle_ChannelNotFound_ReturnsNull()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var query = new GetChannelByIdQuery(channelId);

        _channelRepoMock.Setup(r => r.GetByIdAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Channel?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
