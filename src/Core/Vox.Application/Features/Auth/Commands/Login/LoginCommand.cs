using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthTokensDto>;
