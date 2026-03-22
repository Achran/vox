namespace Vox.Domain.Interfaces.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IServerRepository Servers { get; }
    IChannelRepository Channels { get; }
    IServerMemberRepository ServerMembers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
