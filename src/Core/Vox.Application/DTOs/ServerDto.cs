namespace Vox.Application.DTOs;

public sealed record ServerDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    Guid OwnerId,
    DateTime CreatedAt
);
