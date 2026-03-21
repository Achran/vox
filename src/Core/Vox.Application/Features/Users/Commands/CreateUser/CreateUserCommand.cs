using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string DisplayName,
    string Password
) : IRequest<UserDto>;
