namespace Vox.Application.DTOs;

public sealed record PresenceUserDto(
    string UserId,
    string Status,
    string? DisplayName = null
);
