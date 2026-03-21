using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class Message : BaseEntity
{
    public Guid AuthorId { get; private set; }
    public Guid ChannelId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsEdited { get; private set; }
    public bool IsDeleted { get; private set; }

    private Message() { }

    public static Message Create(Guid authorId, Guid channelId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Message
        {
            AuthorId = authorId,
            ChannelId = channelId,
            Content = content
        };
    }

    public void Edit(string newContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);
        Content = newContent;
        IsEdited = true;
        SetUpdatedAt();
    }

    public void Delete()
    {
        IsDeleted = true;
        SetUpdatedAt();
    }
}
