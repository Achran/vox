using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Infrastructure.Persistence.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly VoxDbContext _context;

    public ChannelRepository(VoxDbContext context)
    {
        _context = context;
    }

    public async Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Channels
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Channel>> GetByServerIdAsync(Guid serverId, CancellationToken cancellationToken = default)
        => await _context.Channels
            .Where(c => c.ServerId == serverId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Channel channel, CancellationToken cancellationToken = default)
        => await _context.Channels.AddAsync(channel, cancellationToken);

    public Task UpdateAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        _context.Channels.Update(channel);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var channel = await _context.Channels.FindAsync([id], cancellationToken);
        if (channel is not null)
        {
            _context.Channels.Remove(channel);
        }
    }
}
