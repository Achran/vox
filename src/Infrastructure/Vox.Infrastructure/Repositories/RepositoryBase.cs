using Microsoft.EntityFrameworkCore;
using Vox.Domain.Repositories;
using Vox.Infrastructure.Data;

namespace Vox.Infrastructure.Repositories;

public abstract class RepositoryBase<T>(VoxDbContext dbContext) : IRepository<T>
    where T : class
{
    protected readonly VoxDbContext DbContext = dbContext;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().FindAsync([id], cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbContext.Set<T>().Update(entity);

    public void Remove(T entity) => DbContext.Set<T>().Remove(entity);
}
