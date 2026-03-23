using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Domain.Interfaces.Repositories;

namespace Vox.Infrastructure.Persistence.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly VoxDbContext _context;

    public MessageRepository(VoxDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Messages.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Message>> GetByChannelIdAsync(
        Guid channelId, int pageSize, DateTime? before, CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Where(m => m.ChannelId == channelId && !m.IsDeleted);

        if (before.HasValue)
        {
            query = query.Where(m => m.CreatedAt < before.Value);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
        => await _context.Messages.AddAsync(message, cancellationToken);
}
