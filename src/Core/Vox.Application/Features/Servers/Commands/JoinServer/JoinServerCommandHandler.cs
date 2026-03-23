using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Servers.Commands.JoinServer;

public sealed class JoinServerCommandHandler : IRequestHandler<JoinServerCommand, ServerDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public JoinServerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServerDto> Handle(JoinServerCommand request, CancellationToken cancellationToken)
    {
        var server = await _unitOfWork.Servers.GetByIdAsync(request.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{request.ServerId}' was not found.");

        server.AddMember(request.UserId);
        await _unitOfWork.Servers.UpdateAsync(server, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ServerDto(
            server.Id,
            server.Name,
            server.Description,
            server.IconUrl,
            server.OwnerId,
            server.CreatedAt
        );
    }
}
