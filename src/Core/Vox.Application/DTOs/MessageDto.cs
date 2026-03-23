namespace Vox.Application.DTOs;

public sealed record MessageDto(
    Guid Id,
    Guid AuthorId,
    Guid ChannelId,
    string Content,
    bool IsEdited,
    DateTime CreatedAt
);
