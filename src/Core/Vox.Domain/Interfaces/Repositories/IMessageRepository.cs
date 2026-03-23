using Vox.Domain.Entities;

namespace Vox.Domain.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Message>> GetByChannelIdAsync(Guid channelId, int pageSize, DateTime? before, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
}
