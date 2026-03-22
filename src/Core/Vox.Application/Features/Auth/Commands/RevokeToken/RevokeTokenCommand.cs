using MediatR;

namespace Vox.Application.Features.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest;
