using MediatR;
using Vox.Application.DTOs;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Users.ExistsByEmailAsync(request.Email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
        }

        var user = User.Create(request.UserName, request.Email, request.DisplayName);
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.Status.ToString(),
            user.CreatedAt
        );
    }
}
