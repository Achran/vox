using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Infrastructure.Persistence.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly VoxDbContext _context;

    public ServerRepository(VoxDbContext context)
    {
        _context = context;
    }

    public async Task<Server?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Servers
            .Include(s => s.Channels)
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Server>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Servers
            .Include(s => s.Channels)
            .Where(s => s.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Server server, CancellationToken cancellationToken = default)
        => await _context.Servers.AddAsync(server, cancellationToken);

    public Task UpdateAsync(Server server, CancellationToken cancellationToken = default)
    {
        _context.Servers.Update(server);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var server = await GetByIdAsync(id, cancellationToken);
        if (server is not null)
        {
            _context.Servers.Remove(server);
        }
    }
}
