using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class Server : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public Guid OwnerId { get; private set; }

    private readonly List<Channel> _channels = [];
    public IReadOnlyCollection<Channel> Channels => _channels.AsReadOnly();

    private Server() { }

    public static Server Create(string name, Guid ownerId, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Server
        {
            Name = name,
            OwnerId = ownerId,
            Description = description
        };
    }

    public Channel AddChannel(string name, ChannelType type = ChannelType.Text)
    {
        var channel = Channel.Create(name, Id, type);
        _channels.Add(channel);
        return channel;
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
