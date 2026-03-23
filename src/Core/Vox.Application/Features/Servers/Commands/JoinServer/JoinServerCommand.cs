using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Servers.Commands.JoinServer;

public sealed record JoinServerCommand(
    Guid ServerId,
    Guid UserId
) : IRequest<ServerDto>;
