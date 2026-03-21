using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class Message : Entity
{
    public string Content { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public Guid ChannelId { get; private set; }
    public bool IsEdited { get; private set; }
    public bool IsDeleted { get; private set; }

    private Message() { }

    public static Message Create(string content, Guid authorId, Guid channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Message
        {
            Content = content,
            AuthorId = authorId,
            ChannelId = channelId
        };
    }

    public void Edit(string newContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);
        Content = newContent;
        IsEdited = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
