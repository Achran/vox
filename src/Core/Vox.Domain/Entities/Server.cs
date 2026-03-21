using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class Server : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public Guid OwnerId { get; private set; }

    private readonly List<Channel> _channels = [];
    private readonly List<ServerMember> _members = [];

    public IReadOnlyCollection<Channel> Channels => _channels.AsReadOnly();
    public IReadOnlyCollection<ServerMember> Members => _members.AsReadOnly();

    private Server() { }

    public static Server Create(string name, Guid ownerId, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var server = new Server
        {
            Name = name,
            OwnerId = ownerId,
            Description = description
        };

        server._channels.Add(Channel.Create("general", ChannelType.Text, server.Id));
        server._channels.Add(Channel.Create("General", ChannelType.Voice, server.Id));
        server._members.Add(ServerMember.Create(ownerId, server.Id, ServerRole.Owner));

        return server;
    }

    public Channel AddChannel(string name, ChannelType type)
    {
        var channel = Channel.Create(name, type, Id);
        _channels.Add(channel);
        SetUpdatedAt();
        return channel;
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            return;
        }

        _members.Add(ServerMember.Create(userId, Id, ServerRole.Member));
        SetUpdatedAt();
    }
}
