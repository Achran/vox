using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Domain.Repositories;
using Vox.Infrastructure.Data;

namespace Vox.Infrastructure.Repositories;

public class ServerRepository(VoxDbContext dbContext)
    : RepositoryBase<Server>(dbContext), IServerRepository
{
    public async Task<IEnumerable<Server>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Servers
            .Where(s => s.OwnerId == userId)
            .ToListAsync(cancellationToken);
}

public class ChannelRepository(VoxDbContext dbContext)
    : RepositoryBase<Channel>(dbContext), IChannelRepository
{
    public async Task<IEnumerable<Channel>> GetByServerIdAsync(Guid serverId, CancellationToken cancellationToken = default)
        => await DbContext.Channels
            .Where(c => c.ServerId == serverId)
            .ToListAsync(cancellationToken);
}

public class MessageRepository(VoxDbContext dbContext)
    : RepositoryBase<Message>(dbContext), IMessageRepository
{
    public async Task<IEnumerable<Message>> GetByChannelIdAsync(
        Guid channelId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        => await DbContext.Messages
            .Where(m => m.ChannelId == channelId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
}
