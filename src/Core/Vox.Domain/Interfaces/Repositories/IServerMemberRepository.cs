using Vox.Domain.Entities;

namespace Vox.Domain.Interfaces.Repositories;

public interface IServerMemberRepository
{
    Task<ServerMember?> GetByUserAndServerAsync(Guid userId, Guid serverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServerMember>> GetByServerIdAsync(Guid serverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServerMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid userId, Guid serverId, CancellationToken cancellationToken = default);
    Task AddAsync(ServerMember member, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, Guid serverId, CancellationToken cancellationToken = default);
    Task UpdateAsync(ServerMember member, CancellationToken cancellationToken = default);
}
