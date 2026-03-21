using Vox.Domain.Entities;

namespace Vox.Domain.Interfaces.Repositories;

public interface IServerRepository
{
    Task<Server?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Server>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Server server, CancellationToken cancellationToken = default);
    Task UpdateAsync(Server server, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
