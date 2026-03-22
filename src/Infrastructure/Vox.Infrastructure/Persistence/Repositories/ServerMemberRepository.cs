using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Infrastructure.Persistence.Repositories;

public class ServerMemberRepository : IServerMemberRepository
{
    private readonly VoxDbContext _context;

    public ServerMemberRepository(VoxDbContext context)
    {
        _context = context;
    }

    public async Task<ServerMember?> GetByUserAndServerAsync(Guid userId, Guid serverId, CancellationToken cancellationToken = default)
        => await _context.ServerMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ServerId == serverId, cancellationToken);

    public async Task<IReadOnlyList<ServerMember>> GetByServerIdAsync(Guid serverId, CancellationToken cancellationToken = default)
        => await _context.ServerMembers
            .Where(m => m.ServerId == serverId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServerMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.ServerMembers
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsMemberAsync(Guid userId, Guid serverId, CancellationToken cancellationToken = default)
        => await _context.ServerMembers
            .AnyAsync(m => m.UserId == userId && m.ServerId == serverId, cancellationToken);

    public async Task AddAsync(ServerMember member, CancellationToken cancellationToken = default)
        => await _context.ServerMembers.AddAsync(member, cancellationToken);

    public async Task RemoveAsync(Guid userId, Guid serverId, CancellationToken cancellationToken = default)
    {
        var member = await GetByUserAndServerAsync(userId, serverId, cancellationToken);
        if (member is not null)
        {
            _context.ServerMembers.Remove(member);
        }
    }

    public Task UpdateAsync(ServerMember member, CancellationToken cancellationToken = default)
    {
        _context.ServerMembers.Update(member);
        return Task.CompletedTask;
    }
}
