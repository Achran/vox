using FluentAssertions;
using Moq;
using Vox.Application.Features.Channels.Queries.GetServerChannels;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Tests.Features.Channels;

public class GetServerChannelsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IChannelRepository> _channelRepoMock = new();
    private readonly GetServerChannelsQueryHandler _handler;

    public GetServerChannelsQueryHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Channels).Returns(_channelRepoMock.Object);
        _handler = new GetServerChannelsQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ServerHasChannels_ReturnsChannelDtos()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var channels = new List<Channel>
        {
            Channel.Create("general", ChannelType.Text, serverId),
            Channel.Create("voice", ChannelType.Voice, serverId)
        };
        var query = new GetServerChannelsQuery(serverId);

        _channelRepoMock.Setup(r => r.GetByServerIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("general");
        result[0].Type.Should().Be("Text");
        result[1].Name.Should().Be("voice");
        result[1].Type.Should().Be("Voice");
    }

    [Fact]
    public async Task Handle_ServerHasNoChannels_ReturnsEmptyList()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var query = new GetServerChannelsQuery(serverId);

        _channelRepoMock.Setup(r => r.GetByServerIdAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Channel>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
