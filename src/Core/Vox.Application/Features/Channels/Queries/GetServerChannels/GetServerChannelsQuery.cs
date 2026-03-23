using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Channels.Queries.GetServerChannels;

public sealed record GetServerChannelsQuery(Guid ServerId) : IRequest<IReadOnlyList<ChannelDto>>;
