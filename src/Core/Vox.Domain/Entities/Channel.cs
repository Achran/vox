using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class Channel : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public ChannelType Type { get; private set; }
    public Guid ServerId { get; private set; }

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private Channel() { }

    public static Channel Create(string name, ChannelType type, Guid serverId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Channel
        {
            Name = name,
            Type = type,
            ServerId = serverId
        };
    }

    public Message PostMessage(Guid authorId, string content)
    {
        var message = Message.Create(authorId, Id, content);
        _messages.Add(message);
        SetUpdatedAt();
        return message;
    }
}

public enum ChannelType
{
    Text,
    Voice
}
