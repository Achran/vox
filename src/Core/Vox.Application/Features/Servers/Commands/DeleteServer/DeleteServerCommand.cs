using MediatR;

namespace Vox.Application.Features.Servers.Commands.DeleteServer;

public sealed record DeleteServerCommand(
    Guid ServerId,
    Guid RequestingUserId
) : IRequest;
