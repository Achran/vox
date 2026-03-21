using Vox.Domain.Entities;

namespace Vox.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

public interface IServerRepository : IRepository<Server>
{
    Task<IEnumerable<Server>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IChannelRepository : IRepository<Channel>
{
    Task<IEnumerable<Channel>> GetByServerIdAsync(Guid serverId, CancellationToken cancellationToken = default);
}

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetByChannelIdAsync(Guid channelId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}
