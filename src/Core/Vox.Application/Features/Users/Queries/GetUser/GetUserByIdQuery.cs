using MediatR;
using Vox.Application.DTOs;

namespace Vox.Application.Features.Users.Queries.GetUser;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;
