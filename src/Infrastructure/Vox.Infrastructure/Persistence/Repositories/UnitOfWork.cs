using Vox.Domain.Interfaces.Repositories;

namespace Vox.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly VoxDbContext _context;

    public IUserRepository Users { get; }
    public IServerRepository Servers { get; }
    public IChannelRepository Channels { get; }
    public IServerMemberRepository ServerMembers { get; }

    public UnitOfWork(VoxDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        Servers = new ServerRepository(context);
        Channels = new ChannelRepository(context);
        ServerMembers = new ServerMemberRepository(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
