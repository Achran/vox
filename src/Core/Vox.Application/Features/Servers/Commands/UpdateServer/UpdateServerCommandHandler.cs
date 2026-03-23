using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Servers.Commands.UpdateServer;

public sealed class UpdateServerCommandHandler : IRequestHandler<UpdateServerCommand, ServerDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServerDto> Handle(UpdateServerCommand request, CancellationToken cancellationToken)
    {
        var server = await _unitOfWork.Servers.GetByIdAsync(request.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{request.ServerId}' was not found.");

        if (server.OwnerId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Only the server owner can update the server.");
        }

        server.Update(request.Name, request.Description);
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
