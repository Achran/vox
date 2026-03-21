using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class Channel : Entity
{
    public string Name { get; private set; } = string.Empty;
    public Guid ServerId { get; private set; }
    public ChannelType Type { get; private set; }
    public string? Topic { get; private set; }

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private Channel() { }

    public static Channel Create(string name, Guid serverId, ChannelType type = ChannelType.Text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Channel
        {
            Name = name,
            ServerId = serverId,
            Type = type
        };
    }

    public void UpdateTopic(string? topic)
    {
        Topic = topic;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum ChannelType
{
    Text,
    Voice,
    Announcement
}
