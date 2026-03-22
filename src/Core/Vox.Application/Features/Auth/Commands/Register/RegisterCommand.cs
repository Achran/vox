using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string DisplayName,
    string Password
) : IRequest<AuthTokensDto>;
