using MediatR;

namespace Vox.Application.Features.Channels.Commands.DeleteChannel;

public sealed record DeleteChannelCommand(
    Guid ChannelId,
    Guid RequestingUserId
) : IRequest;
