namespace Vox.Application.DTOs;

public sealed record LinkedAccountDto(
    string Provider,
    string ProviderKey,
    string? ProviderDisplayName
);
