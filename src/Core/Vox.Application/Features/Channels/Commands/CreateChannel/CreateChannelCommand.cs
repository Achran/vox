using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Channels.Commands.CreateChannel;

public sealed record CreateChannelCommand(
    Guid ServerId,
    string Name,
    string Type,
    Guid RequestingUserId
) : IRequest<ChannelDto>;
