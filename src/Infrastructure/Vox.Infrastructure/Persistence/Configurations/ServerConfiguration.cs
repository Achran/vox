using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vox.Domain.Entities;

namespace Vox.Infrastructure.Persistence.Configurations;

public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.IconUrl)
            .HasMaxLength(2048);

        builder.HasMany(s => s.Channels)
            .WithOne()
            .HasForeignKey(c => c.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Members)
            .WithOne()
            .HasForeignKey(m => m.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
