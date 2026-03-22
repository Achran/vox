using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokensDto>;
