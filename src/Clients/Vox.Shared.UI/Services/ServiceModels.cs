namespace Vox.Shared.UI.Services;

public sealed record ServerResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    Guid OwnerId,
    DateTime CreatedAt);

public sealed record ChannelResponse(
    Guid Id,
    string Name,
    string Type,
    Guid ServerId,
    DateTime CreatedAt);

public sealed record OnlineUserInfo(string UserId, string DisplayName);

public sealed record CreateServerRequest(string Name, string? Description);
public sealed record UpdateServerRequest(string Name, string? Description);
public sealed record CreateChannelRequest(string Name, string Type);
public sealed record UpdateChannelRequest(string Name);

public sealed record MessageResponse(
    Guid Id,
    Guid AuthorId,
    Guid ChannelId,
    string Content,
    bool IsEdited,
    DateTime CreatedAt);
