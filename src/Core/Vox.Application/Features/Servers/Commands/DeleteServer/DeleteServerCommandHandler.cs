using MediatR;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Servers.Commands.DeleteServer;

public sealed class DeleteServerCommandHandler : IRequestHandler<DeleteServerCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteServerCommand request, CancellationToken cancellationToken)
    {
        var server = await _unitOfWork.Servers.GetByIdAsync(request.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Server with ID '{request.ServerId}' was not found.");

        if (server.OwnerId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Only the server owner can delete the server.");
        }

        await _unitOfWork.Servers.DeleteAsync(request.ServerId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
