namespace Vox.Application.DTOs;

public sealed record ChannelDto(
    Guid Id,
    string Name,
    string Type,
    Guid ServerId,
    DateTime CreatedAt
);
