using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.ExternalLogin;

public sealed record ExternalLoginCommand(
    string Provider,
    string ProviderKey,
    string? Email,
    string? DisplayName
) : IRequest<AuthTokensDto>;
