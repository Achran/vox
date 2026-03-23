using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Servers.Commands.CreateServer;

public sealed record CreateServerCommand(
    string Name,
    string? Description,
    Guid OwnerId
) : IRequest<ServerDto>;
