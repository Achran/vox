using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Presence.Queries.GetOnlineServerUsers;

public sealed record GetOnlineServerUsersQuery(Guid ServerId) : IRequest<IReadOnlyList<PresenceUserDto>>;
