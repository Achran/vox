using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Channels.Commands.UpdateChannel;

public sealed record UpdateChannelCommand(
    Guid ChannelId,
    string Name,
    Guid RequestingUserId
) : IRequest<ChannelDto>;
