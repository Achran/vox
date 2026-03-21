namespace Vox.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Status,
    DateTime CreatedAt
);
