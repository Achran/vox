using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Servers.Commands.UpdateServer;

public sealed record UpdateServerCommand(
    Guid ServerId,
    string Name,
    string? Description,
    Guid RequestingUserId
) : IRequest<ServerDto>;
