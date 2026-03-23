using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Servers.Queries.GetServer;

public sealed record GetServerByIdQuery(Guid ServerId) : IRequest<ServerDto?>;
