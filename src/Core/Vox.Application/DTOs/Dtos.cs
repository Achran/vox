namespace Vox.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Status);

public record ServerDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    Guid OwnerId);

public record ChannelDto(
    Guid Id,
    string Name,
    Guid ServerId,
    string Type,
    string? Topic);

public record MessageDto(
    Guid Id,
    string Content,
    Guid AuthorId,
    Guid ChannelId,
    bool IsEdited,
    DateTimeOffset CreatedAt);
