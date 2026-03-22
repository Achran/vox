using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vox.Domain.Entities;
using Vox.Infrastructure.Identity;

namespace Vox.Infrastructure.Persistence;

public class VoxDbContext : IdentityDbContext<ApplicationUser>
{
    public VoxDbContext(DbContextOptions<VoxDbContext> options) : base(options)
    {
    }

    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VoxDbContext).Assembly);
    }
}
