using MediatR;

namespace Vox.Application.Features.Auth.Commands.UnlinkProvider;

public sealed record UnlinkProviderCommand(
    string UserId,
    string Provider
) : IRequest;
