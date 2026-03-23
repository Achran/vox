using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Presence.Queries.GetOnlineChannelUsers;

public sealed record GetOnlineChannelUsersQuery(Guid ChannelId) : IRequest<IReadOnlyList<PresenceUserDto>>;
