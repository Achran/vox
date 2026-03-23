using MediatR;

namespace Vox.Application.Features.Auth.Commands.LinkProvider;

public sealed record LinkProviderCommand(
    string UserId,
    string Provider,
    string ProviderKey
) : IRequest;
