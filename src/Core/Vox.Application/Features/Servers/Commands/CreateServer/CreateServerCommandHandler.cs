using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Servers.Commands.CreateServer;

public sealed class CreateServerCommandHandler : IRequestHandler<CreateServerCommand, ServerDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateServerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServerDto> Handle(CreateServerCommand request, CancellationToken cancellationToken)
    {
        var server = Server.Create(request.Name, request.OwnerId, request.Description);

        await _unitOfWork.Servers.AddAsync(server, cancellationToken);
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
