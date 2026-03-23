using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Channels.Queries.GetChannel;

public sealed record GetChannelByIdQuery(Guid ChannelId) : IRequest<ChannelDto?>;
